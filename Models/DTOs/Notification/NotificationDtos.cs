using System;
using System.Collections.Generic;

namespace ToanHocHay.WebApp.Models.DTOs.Notification
{
    public enum NotificationType { Info, Warning, Success, Error, Reminder }
    public enum NotifyAudience { Student, Parent, Both, Staff }

    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public int? StudentId { get; set; }
        public NotifyAudience Audience { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public NotificationType NotificationType { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationPreferenceDto
    {
        public string RuleKey { get; set; } = "";
        public bool Enabled { get; set; }

        /// <summary>Nhãn tiếng Việt cho các rule đã biết.</summary>
        public string Label => RuleKey switch
        {
            "tab-switch" => "Cảnh báo khi con chuyển tab lúc làm bài",
            "low-score"  => "Nhắc khi điểm bài làm thấp (< 5)",
            "inactivity" => "Nhắc khi con nghỉ học nhiều ngày",
            _ => RuleKey
        };
    }

    public class SetNotificationPreferenceDto
    {
        public string RuleKey { get; set; } = "";
        public bool Enabled { get; set; }
    }
}
