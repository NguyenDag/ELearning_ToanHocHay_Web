using Microsoft.AspNetCore.Mvc;

namespace ToanHocHay.WebApp.Areas.Admin.Controllers
{
    public class DashboardController : AdminBaseController
    {
        public IActionResult Index() => View();
    }
}
