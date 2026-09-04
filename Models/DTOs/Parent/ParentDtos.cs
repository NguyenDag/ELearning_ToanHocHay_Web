using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToanHocHay.WebApp.Models.DTOs.Parent
{
    public enum ParentRelationship { Father, Mother, Guardian, Other }
    public enum LinkStatus { Pending, Active, Revoked }
    public enum ParentInviteStatus { Pending, Accepted, Expired, Cancelled }

    public class ParentInfoDto
    {
        public int ParentId { get; set; }
        public int UserId { get; set; }
        public string? Job { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string ConnectionCode { get; set; } = "";
        public List<ChildDto> Children { get; set; } = new();
    }

    public class ChildDto
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = "";
        public int GradeLevel { get; set; }
        public string Relationship { get; set; } = "";
    }

    public class ParentLinkDto
    {
        public int ParentLinkId { get; set; }
        public int ParentId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public ParentRelationship Relationship { get; set; }
        public LinkStatus Status { get; set; }
        public bool IsPrimaryGuardian { get; set; }
        public DateTime LinkedAt { get; set; }
    }

    public class ChildOverviewDto
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = "";
        public int GradeLevel { get; set; }
        public PackageTier PackageTier { get; set; }
        public int WeeklyStudyMinutes { get; set; }
        public int WeeklyExercisesCompleted { get; set; }
        public decimal WeeklyAverageScore { get; set; }
        public int CurrentStreak { get; set; }
        public bool StudiedToday { get; set; }
    }

    public class CreateParentInviteDto
    {
        [EmailAddress]
        public string? InviteeEmail { get; set; }
        public ParentRelationship Relationship { get; set; } = ParentRelationship.Guardian;
        [Range(1, 30)]
        public int ExpiresInDays { get; set; } = 7;
    }

    public class ParentInviteDto
    {
        public int ParentInviteId { get; set; }
        public string Token { get; set; } = "";
        public string? InviteeEmail { get; set; }
        public ParentInviteStatus Status { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LinkParentInputDto
    {
        [Required] public string Code { get; set; } = "";
        public ParentRelationship Relationship { get; set; } = ParentRelationship.Guardian;
    }
}
