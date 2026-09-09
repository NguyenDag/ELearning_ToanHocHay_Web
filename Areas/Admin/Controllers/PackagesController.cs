using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Services.Admin;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Areas.Admin.Controllers
{
    /// <summary>Gói &amp; giá — SystemAdmin + FinanceManager. Tier cố định.</summary>
    public class PackagesController : FinanceBaseController
    {
        private readonly PackageAdminApiService _packages;

        public PackagesController(PackageAdminApiService packages) => _packages = packages;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vm = await _packages.ListAsync();
            if (GuardListError(vm) is { } redirect) return redirect;
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var p = await _packages.GetAsync(id);
            if (p == null) { this.PushToastError("Không tìm thấy gói."); return RedirectToAction(nameof(Index)); }

            return View(new PackageEditVm
            {
                PackageId = p.PackageId,
                Tier = p.Tier,
                ActiveSubscriberCount = p.ActiveSubscriberCount,
                PackageName = p.PackageName,
                Description = p.Description,
                Price = p.Price,
                DurationDays = p.DurationDays,
                AiHintLimitDaily = p.AiHintLimitDaily,
                UnlimitedAiHint = p.UnlimitedAiHint,
                PersonalizedPath = p.PersonalizedPath,
                MistakeRetry = p.MistakeRetry,
                SmartReminder = p.SmartReminder,
                PrioritySupport = p.PrioritySupport,
                IsActive = p.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PackageEditVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            this.PushToastResult(await _packages.UpdateAsync(id, vm), "Đã lưu gói.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id, bool isActive)
        {
            this.PushToastResult(await _packages.SetActiveAsync(id, isActive),
                isActive ? "Đã bật gói." : "Đã tắt gói.");
            return RedirectToAction(nameof(Index));
        }
    }
}
