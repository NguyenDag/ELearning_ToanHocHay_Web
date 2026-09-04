using System;
using System.Collections.Generic;

namespace ToanHocHay.WebApp.Models.DTOs.Chatbot
{
    public enum ChatSender { User, AI, Staff, System }
    public enum ChatStatus { Bot, WaitingAgent, WithAgent, EscalatedToPhone, Closed }

    public class ChatMessageVm
    {
        public long MessageId { get; set; }
        public int ConversationId { get; set; }
        public ChatSender SenderType { get; set; }
        public string Body { get; set; } = "";
        public DateTime SentAt { get; set; }
    }

    public class ChatTurnResultDto
    {
        public int ConversationId { get; set; }
        public ChatMessageVm? Reply { get; set; }
        public bool AiAvailable { get; set; }
        public List<string>? Options { get; set; }
        public ChatStatus ConversationStatus { get; set; }
    }

    public class ChatConversationVm
    {
        public int ConversationId { get; set; }
        public string? Topic { get; set; }
        public ChatStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public int MessageCount { get; set; }
    }
}
