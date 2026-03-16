// FILE: ToanHocHay.WebApp/Controllers/ParentController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using ToanHocHay.WebApp.Common.Constants;

namespace ToanHocHay.WebApp.Controllers
{
    [Authorize]
    public class ParentController : Controller
    {
        private readonly IHttpClientFactory _factory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ParentController(IHttpClientFactory factory, IHttpContextAccessor httpContextAccessor)
        {
            _factory = factory;
            _httpContextAccessor = httpContextAccessor;
        }

        private HttpClient CreateAuthClient()
        {
            var client = _factory.CreateClient();
            var token = _httpContextAccessor.HttpContext?.Session.GetString("Token")
                      ?? _httpContextAccessor.HttpContext?.User.FindFirst("Token")?.Value;
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

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

            try
            {
                var parentIdStr = User.FindFirst("ParentId")?.Value;
                if (string.IsNullOrEmpty(parentIdStr))
                {
                    ViewData["ConnectionCode"] = "Không tìm thấy mã";
                    return View("~/Views/Parent/Connection.cshtml");
                }

                var client = CreateAuthClient();
                var res = await client.GetAsync(
                    $"{ApiConstant.apiBaseUrl}/api/parent/{parentIdStr}");

                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    var wrapper = JsonSerializer.Deserialize<JsonElement>(json);

                    // BE trả về PascalCase: Data, ConnectionCode, Children
                    if (wrapper.TryGetProperty("Data", out var data))
                    {
                        if (data.TryGetProperty("ConnectionCode", out var code))
                            ViewData["ConnectionCode"] = code.GetString() ?? "--------";

                        if (data.TryGetProperty("Children", out var children) &&
                            children.ValueKind == JsonValueKind.Array)
                        {
                            var students = children.EnumerateArray()
                                .Select(ch => new ConnectedStudentVm
                                {
                                    FullName = ch.TryGetProperty("FullName", out var fn) ? fn.GetString() ?? "" : "",
                                    GradeLevel = ch.TryGetProperty("GradeLevel", out var gl) ? gl.GetInt32() : 6,
                                }).ToList();
                            ViewData["ConnectedStudents"] = students;
                        }
                    }
                }
            }
            catch { }

            return View("~/Views/Parent/Connection.cshtml");
        }

        // GET /Parent/Report
        public IActionResult Report()
        {
            ViewData["ParentName"] = User.Identity?.Name ?? "Phụ huynh";
            return View("~/Views/Parent/Report.cshtml");
        }

        // GET /Parent/GetStudentReport?studentId=xx — AJAX cho Report page
        [HttpGet]
        public async Task<IActionResult> GetStudentReport(int studentId)
        {
            try
            {
                var client = CreateAuthClient();
                var res = await client.GetAsync(
                    $"{ApiConstant.apiBaseUrl}/api/student/{studentId}/dashboard/overview");

                if (!res.IsSuccessStatusCode)
                    return Json(new { success = false, message = "Không tải được dữ liệu" });

                var json = await res.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                return Json(new { success = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET /Parent/GetInfo — AJAX endpoint cho Dashboard & Connection page
        [HttpGet]
        public async Task<IActionResult> GetInfo()
        {
            try
            {
                var parentIdStr = User.FindFirst("ParentId")?.Value;

                if (string.IsNullOrEmpty(parentIdStr))
                    return Json(new { connectionCode = "--------", children = new object[0] });

                var client = CreateAuthClient();
                var res = await client.GetAsync(
                    $"{ApiConstant.apiBaseUrl}/api/parent/{parentIdStr}");

                if (!res.IsSuccessStatusCode)
                    return Json(new { connectionCode = "--------", children = new object[0] });

                var json = await res.Content.ReadAsStringAsync();
                var wrapper = JsonSerializer.Deserialize<JsonElement>(json);

                // BE trả về PascalCase: Data, ConnectionCode, Children
                if (wrapper.TryGetProperty("Data", out var data))
                {
                    var code = data.TryGetProperty("ConnectionCode", out var c)
                        ? c.GetString() ?? "--------"
                        : "--------";

                    var children = new List<object>();

                    if (data.TryGetProperty("Children", out var ch) &&
                        ch.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var child in ch.EnumerateArray())
                        {
                            children.Add(new
                            {
                                fullName = child.TryGetProperty("FullName", out var fn) ? fn.GetString() : "",
                                gradeLevel = child.TryGetProperty("GradeLevel", out var gl) ? gl.GetInt32() : 6,
                            });
                        }
                    }

                    return Json(new { connectionCode = code, children });
                }

                return Json(new { connectionCode = "--------", children = new object[0] });
            }
            catch
            {
                return Json(new { connectionCode = "--------", children = new object[0] });
            }
        }
    }

    public class ConnectedStudentVm
    {
        public string FullName { get; set; } = "";
        public int GradeLevel { get; set; }
    }
}