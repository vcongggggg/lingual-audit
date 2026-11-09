using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Lingual.Modules.MezonBot.Infrastructure.Data;

namespace Lingual.Modules.MezonBot.Services;

public class MezonUserResolver : IMezonUserResolver
{
    private readonly MezonBotDbContext _dbContext;
    private readonly ILogger<MezonUserResolver> _logger;

    public MezonUserResolver(
        MezonBotDbContext dbContext,
        ILogger<MezonUserResolver> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Guid> ResolveUserIdAsync(string mezonUserId, string? displayName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mezonUserId))
        {
            return Guid.Empty;
        }

        var cleanName = string.IsNullOrWhiteSpace(displayName)
            ? $"Learner_{mezonUserId[..Math.Min(6, mezonUserId.Length)]}"
            : displayName.Trim();

        if (_dbContext.Database.IsRelational())
        {
            try
            {
                var connStr = _dbContext.Database.GetConnectionString();
                if (!string.IsNullOrWhiteSpace(connStr))
                {
                    await using var conn = new NpgsqlConnection(connStr);
                    await conn.OpenAsync(cancellationToken);

                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO users (id, mezon_user_id, display_name, status, role, preferred_locale, timezone, total_xp, current_streak, best_streak, freeze_credits, created_at, updated_at)
                        VALUES (gen_random_uuid(), @mezonUserId, @displayName, 'active', 'learner', 'vi-VN', 'Asia/Ho_Chi_Minh', 0, 0, 0, 1, now(), now())
                        ON CONFLICT (mezon_user_id) DO UPDATE SET display_name = EXCLUDED.display_name, updated_at = now()
                        RETURNING id;";

                    var pUserId = cmd.CreateParameter();
                    pUserId.ParameterName = "@mezonUserId";
                    pUserId.Value = mezonUserId;
                    cmd.Parameters.Add(pUserId);

                    var pName = cmd.CreateParameter();
                    pName.ParameterName = "@displayName";
                    pName.Value = cleanName;
                    cmd.Parameters.Add(pName);

                    var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
                    if (scalar is Guid g)
                    {
                        return g;
                    }
                    if (scalar != null && Guid.TryParse(scalar.ToString(), out var parsed))
                    {
                        return parsed;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [MezonUserResolver] Không thể upsert user cho mezonUserId {MezonUserId} vào DB. Fallback sang deterministic GUID.", mezonUserId);
            }
        }

        // Fallback deterministic GUID dựa trên MD5 của mezonUserId để test in-memory không bị nhảy ID ngẫu nhiên
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"mezon_user_{mezonUserId}"));
        return new Guid(hash);
    }
}
