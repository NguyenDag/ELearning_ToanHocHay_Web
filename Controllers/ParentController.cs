// FILE: ToanHocHay.WebApp/Controllers/ParentController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Models.DTOs.Parent;
using ToanHocHay.WebApp.Services;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Controllers
{
    [Authorize]
    public class ParentController : Controller
    {
        private readonly ParentApiService _parents;
        private readonly IDashboardApiService _dashboard;

        public ParentController(ParentApiService parents, IDashboardApiService dashboard)
        {
            _parents = parents;
            _dashboard = dashboard;
        }

        private int? ParentId =>
            int.TryParse(User.FindFirst("ParentId")?.Value, out var id) ? id : null;

        // GET /Parent/Dashboard
        public IActionResult Dashboard()
        {
            ViewData["ParentName"] = User.Identity?.Name ?? "Phụ huynh";
            return View("~/Views/Parent/Dashboard.cshtml");
        }

        // GET /Parent/Connection
        public async Task<IActionResult> Connection()
        {
            ViewData["ParentName"] = User.Identity?.Name ?? "Phụ huynh";
            ViewData["ConnectionCode"] = "--------";

            if (ParentId is { } pid)
            {
                var info = await _parents.GetInfoAsync(pid);
                if (info.IsSuccess && info.Data != null)
                {
                    ViewData["ConnectionCode"] = info.Data.ConnectionCode;
                    ViewData["ConnectedStudents"] = info.Data.Children;
                }
            }
            return View("~/Views/Parent/Connection.cshtml");
        }

        // GET /Parent/Report
        public IActionResult Report()
        {
            ViewData["ParentName"] = User.Identity?.Name ?? "Phụ huynh";
            return View("~/Views/Parent/Report.cshtml");
        }

        // GET /Parent/GetInfo — AJAX cho Dashboard & Connection: mã liên kết + danh sách con
        [HttpGet]
        public async Task<IActionResult> GetInfo()
        {
            if (ParentId is not { } pid)
                return Json(new { connectionCode = "--------", children = Array.Empty<object>() });

            var info = await _parents.GetInfoAsync(pid);
            var children = await _parents.GetChildrenAsync(pid);

            var kids = (children.IsSuccess ? children.Data : null)?
                .Where(l => l.Status == LinkStatus.Active)
                .Select(l => new { studentId = l.StudentId, fullName = l.StudentName, gradeLevel = 0, status = l.Status.ToString() })
                .ToList();

            return Json(new
            {
                connectionCode = info.IsSuccess ? info.Data?.ConnectionCode ?? "--------" : "--------",
                children = (object?)kids ?? Array.Empty<object>()
            });
        }

        // GET /Parent/Overview — AJAX: tổng quan nhiều con (streak, điểm tuần...)
        [HttpGet]
        public async Task<IActionResult> Overview()
        {
            if (ParentId is not { } pid) return Json(Array.Empty<object>());
            var r = await _parents.GetOverviewAsync(pid);
            return Json(r.IsSuccess ? r.Data ?? new() : new());
        }

        // POST /Parent/Invite — mời con qua email / tạo mã lời mời một lần
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(CreateParentInviteDto model)
        {
            if (ParentId is not { } pid)
            {
                TempData[ApiResultExtensions.TempDataError] = "Không xác định được phụ huynh.";
                return RedirectToAction("Connection");
            }

            var r = await _parents.CreateInviteAsync(pid, model);
            if (r.IsSuccess && r.Data != null)
            {
                TempData[ApiResultExtensions.TempDataSuccess] =
                    $"Đã tạo lời mời. Mã: {r.Data.Token} (hết hạn {r.Data.ExpiresAt:dd/MM/yyyy})"
                    + (string.IsNullOrEmpty(model.InviteeEmail) ? "" : $" — đã gửi tới {model.InviteeEmail}.");
            }
            else
            {
                TempData[ApiResultExtensions.TempDataError] = r.DisplayMessage;
            }
            return RedirectToAction("Connection");
        }

        // POST /Parent/RevokeChild — huỷ liên kết một con
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeChild(int studentId)
        {
            if (ParentId is not { } pid) return RedirectToAction("Connection");

            var r = await _parents.RevokeChildAsync(pid, studentId);
            TempData[r.IsSuccess ? ApiResultExtensions.TempDataSuccess : ApiResultExtensions.TempDataError] =
                r.IsSuccess ? "Đã huỷ liên kết với học sinh này." : r.DisplayMessage;
            return RedirectToAction("Connection");
        }

        // GET /Parent/GetStudentReport?studentId=xx — AJAX cho trang Report
        [HttpGet]
        public async Task<IActionResult> GetStudentReport(int studentId)
        {
            var r = await _dashboard.GetStudentDashboardAsync(studentId);

            if (r.IsForbidden)
                return Json(new { success = false, message = "Bạn chưa liên kết (hoặc đã huỷ liên kết) với học sinh này." });
            if (!r.IsSuccess || r.Data == null)
                return Json(new { success = false, message = r.DisplayMessage });

            return Json(new { success = true, data = r.Data });
        }
    }
}
