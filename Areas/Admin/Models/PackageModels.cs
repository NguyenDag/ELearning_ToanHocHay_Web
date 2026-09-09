using System.ComponentModel.DataAnnotations;

namespace ToanHocHay.WebApp.Areas.Admin.Models
{
    public enum PackageTierVm { Free = 0, Standard = 1, Premium = 2, Yearly = 3 }

    public static class PackageEnums
    {
        public static string TierLabel(PackageTierVm t) => t switch
        {
            PackageTierVm.Free => "Miễn phí",
            PackageTierVm.Standard => "Tiêu chuẩn",
            PackageTierVm.Premium => "Cao cấp",
            PackageTierVm.Yearly => "Theo năm",
            _ => t.ToString()
        };

        public static string TierBadge(PackageTierVm t) => t switch
        {
            PackageTierVm.Free => "bg-gray-100 text-gray-600",
            PackageTierVm.Standard => "bg-blue-100 text-blue-700",
            PackageTierVm.Premium => "bg-amber-100 text-amber-700",
            PackageTierVm.Yearly => "bg-purple-100 text-purple-700",
            _ => "bg-gray-100 text-gray-600"
        };
    }

    public class PackageAdminDto
    {
        public int PackageId { get; set; }
        public string PackageName { get; set; } = "";
        public string? Description { get; set; }
        public PackageTierVm Tier { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public int? MaxMembers { get; set; }
        public int? AiHintLimitDaily { get; set; }
        public bool UnlimitedAiHint { get; set; }
        public bool PersonalizedPath { get; set; }
        public bool MistakeRetry { get; set; }
        public bool SmartReminder { get; set; }
        public bool PrioritySupport { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int ActiveSubscriberCount { get; set; }
    }

    public class PackageEditVm
    {
        public int PackageId { get; set; }

        // Chỉ hiển thị — không sửa được.
        public PackageTierVm Tier { get; set; }
        public int ActiveSubscriberCount { get; set; }

        [Required(ErrorMessage = "Nhập tên gói"), MaxLength(100)]
        public string PackageName { get; set; } = "";

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Range(0, 100_000_000, ErrorMessage = "Giá phải từ 0 trở lên")]
        public decimal Price { get; set; }

        [Range(0, 3650, ErrorMessage = "Số ngày không hợp lệ")]
        public int DurationDays { get; set; }

        [Range(0, 1000)]
        public int? AiHintLimitDaily { get; set; }

        public bool UnlimitedAiHint { get; set; }
        public bool PersonalizedPath { get; set; }
        public bool MistakeRetry { get; set; }
        public bool SmartReminder { get; set; }
        public bool PrioritySupport { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class PackageListVm : AdminListVmBase
    {
        public List<PackageAdminDto> Items { get; set; } = new();
    }
}
