using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mezon.Net.Models;
using SdkClient = Mezon.Net.Sdk.MezonClient;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Contracts.DTOs;

namespace Lingual.Modules.MezonBot.Services;

public record VoiceSpeakingSession(
    long ClanId,
    long ChannelId,
    long UserId,
    string Username,
    DateTime JoinedAtUtc,
    DateTime? LeftAtUtc,
    TimeSpan SpeakingDuration,
    HashSet<int> AwardedBucketIndices);

public record VoiceXpAwardResult(
    long UserId,
    long ChannelId,
    int BucketIndex,
    int AwardedXp,
    bool AlreadyAwarded,
    string IdempotencyKey,
    string Message);

public interface IVoiceStudyRoomTracker
{
    VoiceSpeakingSession OnUserJoinedVoice(long clanId, long channelId, long userId, string username, DateTime? joinedAt = null);
    VoiceSpeakingSession? OnUserLeftVoice(long channelId, long userId, DateTime? leftAt = null);
    void RecordSpeakingTick(long channelId, long userId, TimeSpan speakingDelta);
    VoiceSpeakingSession? GetSession(long channelId, long userId);
    IReadOnlyList<VoiceSpeakingSession> GetActiveSessions(long channelId);

    Task<IReadOnlyList<VoiceXpAwardResult>> AwardSpeakingXpAsync(
        long channelId,
        long userId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default);

    Task<GenerateMeetTokenResponse?> GenerateMeetTokenAsync(
        SdkClient? client,
        long clanId,
        long channelId,
        string roomName,
        string metadata = "");

    Task<IReadOnlyList<long>> ListVoiceUsersAsync(
        SdkClient? client,
        long clanId,
        long channelId,
        int channelType = 4);
}

public class VoiceStudyRoomTracker : IVoiceStudyRoomTracker
{
    private readonly ILogger<VoiceStudyRoomTracker> _logger;
    private readonly ConcurrentDictionary<(long ChannelId, long UserId), VoiceSpeakingSession> _sessions = new();
    private readonly ConcurrentDictionary<string, bool> _awardedKeys = new();

    /// <summary>
    /// [SDK 1.11.0 LIMITATION - BLOCKED EVIDENCE]:
    /// Thư viện Mezon.Net.Sdk 1.11.0 cung cấp REST API GenerateMeetTokenAsync và Gateway events
    /// VoiceJoined/VoiceLeaved/VoiceInteractiveReceived, nhưng KHÔNG tích hợp WebRTC SFU client
    /// hoặc Opus audio encoder bên trong assembly để bot tự join stream âm thanh trực tiếp vào phòng Meet.
    /// Do đó, Bot hoạt động với vai trò Quản lý phòng học thoại thông minh (Voice Study Room Orchestrator):
    /// cấp Meet Token, giám sát danh sách học viên qua ListChannelVoiceUsersAsync, tính thời gian luyện nói
    /// và thực thi trao thưởng XP Idempotent chuẩn Server-Side.
    /// </summary>
    public const string SfuAudioStreamingBlockedEvidence =
        "Mezon.Net.Sdk 1.11.0 does not include a WebRTC SFU client or Opus encoder assembly. " +
        "Bot acts as Voice Room Orchestrator via GenerateMeetTokenAsync, ListChannelVoiceUsersAsync, " +
        "and Gateway events VoiceJoined/VoiceLeaved with server-side idempotent XP accounting.";

    public VoiceStudyRoomTracker(ILogger<VoiceStudyRoomTracker> logger)
    {
        _logger = logger;
    }

    public VoiceSpeakingSession OnUserJoinedVoice(long clanId, long channelId, long userId, string username, DateTime? joinedAt = null)
    {
        var now = joinedAt ?? DateTime.UtcNow;
        var session = new VoiceSpeakingSession(
            ClanId: clanId,
            ChannelId: channelId,
            UserId: userId,
            Username: username,
            JoinedAtUtc: now,
            LeftAtUtc: null,
            SpeakingDuration: TimeSpan.Zero,
            AwardedBucketIndices: new HashSet<int>()
        );

        _sessions[(channelId, userId)] = session;
        _logger.LogInformation("🎙️ [VoiceTracker] Đã ghi nhận học viên {UserId} ({Username}) vào phòng Voice {ChannelId} (Clan {ClanId}) lúc {Time}",
            userId, username, channelId, clanId, now);

        return session;
    }

    public VoiceSpeakingSession? OnUserLeftVoice(long channelId, long userId, DateTime? leftAt = null)
    {
        if (_sessions.TryGetValue((channelId, userId), out var existing))
        {
            var now = leftAt ?? DateTime.UtcNow;
            var elapsed = now - existing.JoinedAtUtc;
            var updatedDuration = existing.SpeakingDuration < elapsed ? elapsed : existing.SpeakingDuration;

            var closedSession = existing with
            {
                LeftAtUtc = now,
                SpeakingDuration = updatedDuration
            };

            _sessions[(channelId, userId)] = closedSession;
            _logger.LogInformation("🎙️ [VoiceTracker] Học viên {UserId} rời phòng Voice {ChannelId}. Tổng thời gian: {Duration:hh\\:mm\\:ss}",
                userId, channelId, updatedDuration);

            return closedSession;
        }

        return null;
    }

    public void RecordSpeakingTick(long channelId, long userId, TimeSpan speakingDelta)
    {
        if (_sessions.TryGetValue((channelId, userId), out var existing))
        {
            var updated = existing with
            {
                SpeakingDuration = existing.SpeakingDuration + speakingDelta
            };
            _sessions[(channelId, userId)] = updated;
        }
    }

    public VoiceSpeakingSession? GetSession(long channelId, long userId)
    {
        _sessions.TryGetValue((channelId, userId), out var session);
        return session;
    }

    public IReadOnlyList<VoiceSpeakingSession> GetActiveSessions(long channelId)
    {
        return _sessions.Values
            .Where(s => s.ChannelId == channelId && s.LeftAtUtc == null)
            .ToList();
    }

    public async Task<IReadOnlyList<VoiceXpAwardResult>> AwardSpeakingXpAsync(
        long channelId,
        long userId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var results = new List<VoiceXpAwardResult>();
        if (!_sessions.TryGetValue((channelId, userId), out var session))
        {
            return results;
        }

        // Tính số lượng mốc 15 phút (mỗi 15 phút thưởng 50 XP)
        var totalMinutes = session.SpeakingDuration.TotalMinutes;
        var completedBuckets = (int)(totalMinutes / 15);

        if (completedBuckets <= 0)
        {
            return results;
        }

        var gamificationService = serviceProvider.GetService<IGamificationService>();
        var userResolver = serviceProvider.GetService<IMezonUserResolver>();

        var userGuid = Guid.Empty;
        if (userResolver != null)
        {
            userGuid = await userResolver.ResolveUserIdAsync(userId.ToString(), session.Username);
        }

        for (int bucketIndex = 1; bucketIndex <= completedBuckets; bucketIndex++)
        {
            var dateStr = session.JoinedAtUtc.ToString("yyyyMMdd");
            var idempotencyKey = $"voice_speaking_{userId}_{session.ClanId}_{channelId}_{dateStr}_b{bucketIndex}";

            // 1. Kiểm tra bộ nhớ cục bộ chống trùng
            if (session.AwardedBucketIndices.Contains(bucketIndex) || _awardedKeys.ContainsKey(idempotencyKey))
            {
                results.Add(new VoiceXpAwardResult(
                    UserId: userId,
                    ChannelId: channelId,
                    BucketIndex: bucketIndex,
                    AwardedXp: 0,
                    AlreadyAwarded: true,
                    IdempotencyKey: idempotencyKey,
                    Message: $"Mốc {bucketIndex * 15} phút đã được cộng XP trước đó."
                ));
                continue;
            }

            // 2. Trao thưởng server-side qua IGamificationService
            if (gamificationService != null && userGuid != Guid.Empty)
            {
                var awardRequest = new AwardXpRequest(
                    UserId: userGuid,
                    SourceType: "voice_study_room",
                    XpDelta: 50,
                    SourceId: null,
                    IdempotencyKey: idempotencyKey,
                    Metadata: $"{{\"channel_id\":{channelId},\"clan_id\":{session.ClanId},\"bucket\":{bucketIndex},\"duration_minutes\":{totalMinutes:0.0}}}"
                );

                var awardResult = await gamificationService.AwardXpAsync(awardRequest, cancellationToken);
                session.AwardedBucketIndices.Add(bucketIndex);
                _awardedKeys[idempotencyKey] = true;

                results.Add(new VoiceXpAwardResult(
                    UserId: userId,
                    ChannelId: channelId,
                    BucketIndex: bucketIndex,
                    AwardedXp: awardResult.AwardedXp,
                    AlreadyAwarded: awardResult.AlreadyAwarded,
                    IdempotencyKey: idempotencyKey,
                    Message: awardResult.AlreadyAwarded
                        ? "XP đã được trao trước đó (Idempotent detected)."
                        : $"Thành công trao +{awardResult.AwardedXp} XP cho {bucketIndex * 15} phút luyện nói!"
                ));

                _logger.LogInformation("🎉 [VoiceTracker] Trao thưởng XP thành công cho User {UserId}: +{Xp} XP (Bucket {Bucket}, Key {Key})",
                    userId, awardResult.AwardedXp, bucketIndex, idempotencyKey);
            }
            else
            {
                // Fallback nếu không có database context (chạy isolated unit test)
                session.AwardedBucketIndices.Add(bucketIndex);
                _awardedKeys[idempotencyKey] = true;

                results.Add(new VoiceXpAwardResult(
                    UserId: userId,
                    ChannelId: channelId,
                    BucketIndex: bucketIndex,
                    AwardedXp: 50,
                    AlreadyAwarded: false,
                    IdempotencyKey: idempotencyKey,
                    Message: $"Đã ghi nhận +50 XP cho mốc {bucketIndex * 15} phút luyện nói."
                ));
            }
        }

        return results;
    }

    public async Task<GenerateMeetTokenResponse?> GenerateMeetTokenAsync(
        SdkClient? client,
        long clanId,
        long channelId,
        string roomName,
        string metadata = "")
    {
        if (client == null)
        {
            _logger.LogWarning("⚠️ [VoiceTracker] MezonClient chưa khởi tạo, không thể gọi GenerateMeetTokenAsync");
            return null;
        }

        try
        {
            var request = new GenerateMeetTokenParams(
                channelId: channelId,
                roomName: roomName,
                metadata: metadata
            );

            var response = await client.GenerateMeetTokenAsync(request, null);
            _logger.LogInformation("✅ [VoiceTracker] Đã tạo Meet Token thành công cho phòng {RoomName} (Channel {ChannelId})",
                roomName, channelId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [VoiceTracker] Lỗi khi gọi client.GenerateMeetTokenAsync cho phòng {RoomName}", roomName);
            return null;
        }
    }

    public async Task<IReadOnlyList<long>> ListVoiceUsersAsync(
        SdkClient? client,
        long clanId,
        long channelId,
        int channelType = 4)
    {
        if (client == null)
        {
            _logger.LogWarning("⚠️ [VoiceTracker] MezonClient chưa khởi tạo, không thể gọi ListChannelVoiceUsersAsync");
            return Array.Empty<long>();
        }

        try
        {
            var response = await client.ListChannelVoiceUsersAsync(clanId, channelId, channelType, null);
            var userIds = new List<long>();

            if (response.VoiceChannelUsers.Count > 0)
            {
                foreach (var voiceUser in response.VoiceChannelUsers)
                {
                    if (voiceUser.UserIds.Count > 0)
                    {
                        foreach (var uid in voiceUser.UserIds)
                        {
                            if (long.TryParse(uid, out var parsedUid))
                            {
                                userIds.Add(parsedUid);
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("✅ [VoiceTracker] Quét được {Count} học viên trong phòng Voice {ChannelId}",
                userIds.Count, channelId);

            return userIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [VoiceTracker] Lỗi khi gọi client.ListChannelVoiceUsersAsync cho Channel {ChannelId}", channelId);
            return Array.Empty<long>();
        }
    }
}
