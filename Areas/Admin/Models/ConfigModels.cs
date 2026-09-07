namespace ToanHocHay.WebApp.Areas.Admin.Models
{
    public class SystemConfigItemDto
    {
        public string ConfigKey { get; set; } = "";
        public string? ConfigValue { get; set; }
        public string ConfigType { get; set; } = "String";
        public string? ConfigGroup { get; set; }
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? UpdatedByName { get; set; }

        public string InputType => ConfigType switch
        {
            "Int" or "Decimal" => "number",
            _ => "text"
        };
        public bool IsBool => ConfigType == "Bool";
    }

    public class ConfigIndexVm
    {
        public List<IGrouping<string, SystemConfigItemDto>> Groups { get; set; } = new();
        public string? ActiveGroup { get; set; }
    }
}
