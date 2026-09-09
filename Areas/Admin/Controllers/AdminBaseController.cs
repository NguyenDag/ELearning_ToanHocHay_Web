using Microsoft.AspNetCore.Authorization;

namespace ToanHocHay.WebApp.Areas.Admin.Controllers
{
    /// <summary>Khu quản trị đầy đủ — chỉ <c>SystemAdmin</c>.</summary>
    [Authorize(Roles = "SystemAdmin")]
    public abstract class AdminBaseController : AdminAreaControllerBase
    {
    }
}
