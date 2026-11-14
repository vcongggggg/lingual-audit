using System;
using System.Collections.Generic;

namespace Lingual.Modules.MezonBot;

/// <summary>
/// Cấu hình cho tính năng Scheduled Clan Quiz (đố vui tự động theo khung giờ).
/// </summary>
public class ScheduledQuizOptions
{
    public const string SectionName = "ScheduledQuiz";

    /// <summary>
    /// Bật/tắt tính năng Scheduled Clan Quiz.
    /// </summary>
    public bool Enabled { get; set; } = true;

    private List<string> _timeSlots = new();
    /// <summary>
    /// Danh sách các khung giờ cố định trong ngày theo giờ Việt Nam (format HH:mm).
    /// Mặc định: 09:00 (Sáng khởi động), 12:00 (Trưa giải lao), 20:30 (Tối ôn tập).
    /// </summary>
    public List<string> TimeSlots
    {
        get => _timeSlots.Count > 0 ? _timeSlots : new List<string> { "09:00", "12:00", "20:30" };
        set => _timeSlots = value;
    }

    /// <summary>
    /// ID kênh Mezon mục tiêu để phát động Quiz. BẮT BUỘC phải lớn hơn 0 nếu Enabled = true.
    /// Fail-fast ngay khi khởi động nếu TargetChannelId <= 0.
    /// </summary>
    public long TargetChannelId { get; set; } = 2116390031126958080L;

    /// <summary>
    /// Thời lượng mở phiên Quiz (giây). Mặc định: 60 giây.
    /// </summary>
    public int DurationSeconds { get; set; } = 60;

    /// <summary>
    /// Điểm thưởng cho người trả lời ĐÚNG và NHANH NHẤT (Winner). Mặc định: +15 XP.
    /// </summary>
    public int XpWinner { get; set; } = 15;

    /// <summary>
    /// Điểm thưởng cho người trả lời ĐÚNG tiếp theo. Mặc định: +5 XP.
    /// </summary>
    public int XpCorrect { get; set; } = 5;

    /// <summary>
    /// Điểm thưởng tham gia (trả lời sai = 0 XP, KHÔNG có điểm an ủi).
    /// </summary>
    public int XpParticipation { get; set; } = 0;

    /// <summary>
    /// Múi giờ hệ thống (mặc định: Asia/Ho_Chi_Minh).
    /// </summary>
    public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";

    private List<string> _adminUserIds = new();
    /// <summary>
    /// Danh sách User ID quản trị viên được phép thực thi lệnh `/quiz trigger`.
    /// </summary>
    public List<string> AdminUserIds
    {
        get => _adminUserIds.Count > 0 ? _adminUserIds : new List<string>
        {
            "2100896006294999040", // vancong030809 (Admin / Owner)
            "1794149317681",
            "1793710923342"
        };
        set => _adminUserIds = value;
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ của cấu hình theo nguyên tắc Fail-Fast.
    /// </summary>
    public void Validate()
    {
        if (Enabled && TargetChannelId <= 0)
        {
            throw new InvalidOperationException("TargetChannelId bắt buộc phải được cấu hình (lớn hơn 0) khi bật tính năng Scheduled Clan Quiz. Hệ thống fail-fast để tránh tự đoán kênh.");
        }
    }

    /// <summary>
    /// Lấy đối tượng TimeZoneInfo chuẩn cho múi giờ Việt Nam.
    /// </summary>
    public TimeZoneInfo GetTimeZone()
    {
        return ResolveTimeZone(TimeZoneId);
    }

    public static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); } catch { }
        }
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); } catch { }
        try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); } catch { }
        return TimeZoneInfo.CreateCustomTimeZone("Asia/Ho_Chi_Minh", TimeSpan.FromHours(7), "Vietnam Standard Time", "Vietnam Standard Time");
    }
}
