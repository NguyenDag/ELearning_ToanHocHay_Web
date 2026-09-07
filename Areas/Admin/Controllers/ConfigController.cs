using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Services.Admin;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Areas.Admin.Controllers
{
    public class ConfigController : AdminBaseController
    {
        private readonly ConfigAdminApiService _config;

        public ConfigController(ConfigAdminApiService config) => _config = config;

        [HttpGet]
        public async Task<IActionResult> Index(string? group)
        {
            var all = await _config.GetAllAsync();
            var groups = all
                .GroupBy(c => c.ConfigGroup ?? "khác")
                .OrderBy(g => g.Key)
                .ToList();
            return View(new ConfigIndexVm
            {
                Groups = groups,
                ActiveGroup = group ?? groups.FirstOrDefault()?.Key
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Set(string key, string? value, string? group)
        {
            this.PushToastResult(await _config.SetAsync(key, value), $"Đã cập nhật “{key}”.");
            return RedirectToAction(nameof(Index), new { group });
        }
    }
}
