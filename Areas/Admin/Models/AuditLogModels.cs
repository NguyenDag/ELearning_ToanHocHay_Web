namespace ToanHocHay.WebApp.Areas.Admin.Models
{
    public class AuditLogItemDto
    {
        public long LogId { get; set; }
        public int? UserId { get; set; }
        public string? ActorName { get; set; }
        public string? ActorEmail { get; set; }
        public string Action { get; set; } = "";
        public string EntityType { get; set; } = "";
        public int? EntityId { get; set; }
        public string? OldValueJson { get; set; }
        public string? NewValueJson { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AuditLogFacetsDto
    {
        public List<string> EntityTypes { get; set; } = new();
        public List<string> Actions { get; set; } = new();
    }

    public class AuditLogFilterVm
    {
        public string? EntityType { get; set; }
        public string? Action { get; set; }
        public int? UserId { get; set; }
        public string? Q { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class AuditLogIndexVm
    {
        public AuditLogFilterVm Filter { get; set; } = new();
        public List<AuditLogItemDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public AuditLogFacetsDto Facets { get; set; } = new();
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(Total / (double)PageSize) : 1;
    }
}
