using Microsoft.AspNetCore.Authorization;

namespace ToanHocHay.WebApp.Areas.Admin.Controllers
{
    /// <summary>Khu tài chính — <c>SystemAdmin</c> hoặc <c>FinanceManager</c> (gói &amp; giá, doanh thu, giao dịch).</summary>
    [Authorize(Roles = "SystemAdmin,FinanceManager")]
    public abstract class FinanceBaseController : AdminAreaControllerBase
    {
    }
}
