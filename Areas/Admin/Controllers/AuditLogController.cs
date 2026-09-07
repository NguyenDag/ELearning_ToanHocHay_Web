using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Services.Admin;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Areas.Admin.Controllers
{
    public class AuditLogController : AdminBaseController
    {
        private readonly AuditLogAdminApiService _audit;

        public AuditLogController(AuditLogAdminApiService audit) => _audit = audit;

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] AuditLogFilterVm filter)
        {
            if (filter.Page < 1) filter.Page = 1;
            if (filter.PageSize is < 1 or > 200) filter.PageSize = 50;
            var vm = await _audit.ListAsync(filter);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Export([FromQuery] AuditLogFilterVm filter)
        {
            var file = await _audit.ExportCsvAsync(filter);
            if (file == null)
            {
                this.PushToastError("Không xuất được nhật ký. Vui lòng thử lại.");
                return RedirectToAction(nameof(Index), filter);
            }
            return File(file.Value.Bytes, "text/csv", file.Value.FileName);
        }
    }
}
