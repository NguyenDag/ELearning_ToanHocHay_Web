// ToanHocHay.WebApp/Controllers/PackageController.cs

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services;

namespace ToanHocHay.WebApp.Controllers
{
    public class PackageController : Controller
    {
        private readonly PackageApiService _packageService;
        private readonly SubscriptionApiService _subscriptionService;

        public PackageController(PackageApiService packageService, SubscriptionApiService subscriptionService)
        {
            _packageService = packageService;
            _subscriptionService = subscriptionService;
        }

        // GET /Package — Trang danh sách gói
        public async Task<IActionResult> Index(string? plan)
        {
            var packages = await _packageService.GetAllPackagesAsync();
            ViewData["PreselectedPlan"] = plan ?? "";
            return View(packages);
        }

        // GET /Package/Checkout/1 — Trang thanh toán gói cụ thể
        [HttpGet]
        public async Task<IActionResult> Checkout(int id)
        {
            if (!User.Identity!.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            var package = await _packageService.GetPackageByIdAsync(id);
            if (package == null)
                return RedirectToAction("Index");

            var studentIdClaim = User.FindFirst("StudentId");
            if (studentIdClaim == null)
                return RedirectToAction("Index");

            int studentId = int.Parse(studentIdClaim.Value);

            // Tạo subscription + lấy QR
            var result = await _subscriptionService.CreateSubscriptionAsync(
                studentId, id, package.Price);

            if (result == null)
            {
                TempData["Error"] = "Không thể tạo đơn thanh toán. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }

            ViewData["Package"] = package;
            ViewData["SubscriptionId"] = result.SubscriptionId;
            ViewData["QrUrl"] = result.QrUrl;
            return View(package);
        }

        // GET /Package/CheckStatus/11 — Polling từ JS
        [HttpGet]
        public async Task<IActionResult> CheckStatus(int subscriptionId)
        {
            var status = await _subscriptionService.GetSubscriptionStatusAsync(subscriptionId);
            if (status == null)
                return Json(new { status = "error" });

            return Json(new
            {
                status = status.Status,           // "Pending" | "Active"
                endDate = status.EndDate?.ToString("dd/MM/yyyy")
            });
        }
    }
}   