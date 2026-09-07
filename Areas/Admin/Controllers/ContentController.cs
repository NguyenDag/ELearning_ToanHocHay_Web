using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services.Admin;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Areas.Admin.Controllers
{
    public class ContentController : AdminBaseController
    {
        private readonly ContentAdminApiService _content;
        private readonly CatalogAdminApiService _catalog;

        public ContentController(ContentAdminApiService content, CatalogAdminApiService catalog)
        {
            _content = content;
            _catalog = catalog;
        }

        // ================= CATALOG =================

        [HttpGet]
        public async Task<IActionResult> Catalog()
            => View(await _content.GetCatalogAsync());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSubject(int? id, string code, string name, string slug, string? description, int displayOrder, bool isActive)
        {
            this.PushToastResult(await _content.SaveSubjectAsync(id, new { Code = code, Name = name, Slug = slug, Description = description, DisplayOrder = displayOrder, IsActive = isActive }),
                id is > 0 ? "Đã cập nhật môn học." : "Đã thêm môn học.");
            return RedirectToAction(nameof(Catalog));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGrade(int? id, string code, string name, EducationStage stage, int displayOrder, bool isActive)
        {
            this.PushToastResult(await _content.SaveGradeAsync(id, new { Code = code, Name = name, Stage = stage, DisplayOrder = displayOrder, IsActive = isActive }),
                id is > 0 ? "Đã cập nhật khối lớp." : "Đã thêm khối lớp.");
            return RedirectToAction(nameof(Catalog));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveFramework(int? id, string code, string name, string? publisher, bool isActive)
        {
            this.PushToastResult(await _content.SaveFrameworkAsync(id, new { Code = code, Name = name, Publisher = publisher, IsActive = isActive }),
                id is > 0 ? "Đã cập nhật bộ sách." : "Đã thêm bộ sách.");
            return RedirectToAction(nameof(Catalog));
        }

        // ================= COURSES =================

        [HttpGet]
        public async Task<IActionResult> Courses(int? subjectId, int? gradeLevelId)
        {
            var vm = new CourseListVm
            {
                Courses = await _content.ListCoursesAsync(subjectId, gradeLevelId),
                Subjects = await _catalog.GetSubjectsAsync(),
                Grades = await _catalog.GetGradeLevelsAsync(),
                SubjectId = subjectId,
                GradeLevelId = gradeLevelId
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCourse()
        {
            await FillCatalogAsync();
            return View("CourseForm", new CourseEditVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(CourseEditVm vm)
        {
            if (!ModelState.IsValid) { await FillCatalogAsync(); return View("CourseForm", vm); }
            var r = await _content.CreateCourseAsync(vm);
            if (r.IsSuccess)
            {
                this.PushToastSuccess("Đã tạo khoá học.");
                return RedirectToAction(nameof(Course), new { id = r.Data!.CourseId });
            }
            this.ShowToastError(r);
            await FillCatalogAsync();
            return View("CourseForm", vm);
        }

        [HttpGet]
        public async Task<IActionResult> EditCourse(int id)
        {
            var c = await _content.GetCourseAsync(id);
            if (c == null) { this.PushToastError("Không tìm thấy khoá học."); return RedirectToAction(nameof(Courses)); }
            await FillCatalogAsync();
            ViewBag.CourseId = id;
            return View("CourseForm", new CourseEditVm
            {
                CourseId = c.CourseId,
                SubjectId = c.SubjectId,
                GradeLevelId = c.GradeLevelId,
                FrameworkId = c.FrameworkId,
                Title = c.Title,
                Slug = c.Slug,
                Description = c.Description,
                ThumbnailUrl = c.ThumbnailUrl,
                ListPrice = c.ListPrice,
                SalePrice = c.SalePrice,
                IsPurchasable = c.IsPurchasable,
                AccessDurationDays = c.AccessDurationDays,
                DisplayOrder = c.DisplayOrder
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(int id, CourseEditVm vm)
        {
            if (!ModelState.IsValid) { await FillCatalogAsync(); ViewBag.CourseId = id; return View("CourseForm", vm); }
            this.PushToastResult(await _content.UpdateCourseAsync(id, vm), "Đã lưu khoá học.");
            return RedirectToAction(nameof(Course), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveCourse(int id)
        {
            this.PushToastResult(await _content.ArchiveCourseAsync(id), "Đã lưu trữ khoá học.");
            return RedirectToAction(nameof(Course), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnarchiveCourse(int id)
        {
            this.PushToastResult(await _content.UnarchiveCourseAsync(id), "Đã bỏ lưu trữ khoá học.");
            return RedirectToAction(nameof(Course), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Course(int id)
        {
            var c = await _content.GetCourseAsync(id);
            if (c == null) { this.PushToastError("Không tìm thấy khoá học."); return RedirectToAction(nameof(Courses)); }
            return View(new CourseDetailVm
            {
                Course = c,
                Versions = await _content.ListVersionsAsync(id)
            });
        }

        // ================= VERSIONS =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVersion(int courseId, string? label, int? cloneFromVersionId)
        {
            var r = await _content.CreateVersionAsync(courseId, label, cloneFromVersionId);
            this.PushToastResult(r, "Đã tạo phiên bản mới.");
            return r.IsSuccess
                ? RedirectToAction(nameof(Version), new { versionId = r.Data!.CourseVersionId })
                : RedirectToAction(nameof(Course), new { id = courseId });
        }

        [HttpGet]
        public async Task<IActionResult> Version(int versionId, int? courseId, int? nodeId)
        {
            var cid = courseId ?? await ResolveCourseIdAsync(versionId);
            var course = cid is int c ? await _content.GetCourseAsync(c) : null;
            var versions = cid is int c2 ? await _content.ListVersionsAsync(c2) : new List<CourseVersionDto>();

            var vm = new VersionWorkspaceVm
            {
                Course = course ?? new CourseAdminDto { CourseId = cid ?? 0 },
                Version = versions.FirstOrDefault(v => v.CourseVersionId == versionId)
                          ?? new CourseVersionDto { CourseVersionId = versionId },
                Tree = await _content.GetTreeAsync(versionId),
                Reviews = await _content.GetVersionReviewsAsync(versionId),
                SelectedNode = nodeId.HasValue ? await _content.GetNodeAsync(nodeId.Value) : null
            };
            return View(vm);
        }

        private async Task<int?> ResolveCourseIdAsync(int versionId)
        {
            // Duyệt các khoá học để tìm phiên bản. Chỉ dùng khi mở trực tiếp bằng versionId.
            var courses = await _content.ListCoursesAsync(null, null);
            foreach (var c in courses)
            {
                var vs = await _content.ListVersionsAsync(c.CourseId);
                if (vs.Any(v => v.CourseVersionId == versionId)) return c.CourseId;
            }
            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitVersion(int versionId)
        {
            this.PushToastResult(await _content.SubmitVersionAsync(versionId), "Đã gửi phiên bản để duyệt.");
            return RedirectToAction(nameof(Version), new { versionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewVersion(int versionId, ReviewDecision decision, string? summary)
        {
            this.PushToastResult(await _content.ReviewVersionAsync(versionId, decision, summary),
                decision == ReviewDecision.Approve ? "Đã duyệt phiên bản." : "Đã ghi nhận đánh giá.");
            return RedirectToAction(nameof(Version), new { versionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishVersion(int versionId)
        {
            this.PushToastResult(await _content.PublishVersionAsync(versionId), "Đã xuất bản phiên bản.");
            return RedirectToAction(nameof(Version), new { versionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveVersion(int versionId)
        {
            this.PushToastResult(await _content.ArchiveVersionAsync(versionId), "Đã lưu trữ phiên bản.");
            return RedirectToAction(nameof(Version), new { versionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveComment(int commentId, int versionId)
        {
            this.PushToastResult(await _content.ResolveCommentAsync(commentId), "Đã đánh dấu đã xử lý.");
            return RedirectToAction(nameof(Version), new { versionId });
        }

        // ================= NODES =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNode(int versionId, NodeType nodeType, string title, int? parentNodeId, bool isFree, int? durationMinutes)
        {
            this.PushToastResult(await _content.CreateNodeAsync(versionId, nodeType, title, parentNodeId, isFree, durationMinutes),
                "Đã thêm mục nội dung.");
            return RedirectToAction(nameof(Version), new { versionId, nodeId = parentNodeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNode(int versionId, int nodeId, string? title, string? slug, bool isFree, bool isHidden, int? durationMinutes)
        {
            this.PushToastResult(await _content.UpdateNodeAsync(nodeId, title, slug, isFree, isHidden, durationMinutes), "Đã lưu mục nội dung.");
            return RedirectToAction(nameof(Version), new { versionId, nodeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNode(int versionId, int nodeId)
        {
            this.PushToastResult(await _content.DeleteNodeAsync(nodeId), "Đã xoá mục nội dung.");
            return RedirectToAction(nameof(Version), new { versionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveNode(int versionId, int nodeId, int? newParentNodeId, int? orderIndex)
        {
            this.PushToastResult(await _content.MoveNodeAsync(nodeId, newParentNodeId, orderIndex), "Đã di chuyển mục.");
            return RedirectToAction(nameof(Version), new { versionId, nodeId });
        }

        // ================= BLOCKS =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBlock(int versionId, int nodeId, LessonBlockType blockType, string? contentText, string? contentUrl, string? metadataJson, int? orderIndex)
        {
            this.PushToastResult(await _content.AddBlockAsync(nodeId, blockType, contentText, contentUrl, metadataJson, orderIndex), "Đã thêm block.");
            return RedirectToAction(nameof(Version), new { versionId, nodeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBlock(int versionId, int nodeId, int blockId, LessonBlockType blockType, string? contentText, string? contentUrl, string? metadataJson, int? orderIndex)
        {
            this.PushToastResult(await _content.UpdateBlockAsync(blockId, blockType, contentText, contentUrl, metadataJson, orderIndex), "Đã lưu block.");
            return RedirectToAction(nameof(Version), new { versionId, nodeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBlock(int versionId, int nodeId, int blockId)
        {
            this.PushToastResult(await _content.DeleteBlockAsync(blockId), "Đã xoá block.");
            return RedirectToAction(nameof(Version), new { versionId, nodeId });
        }

        private async Task FillCatalogAsync()
        {
            ViewBag.Subjects = await _catalog.GetSubjectsAsync();
            ViewBag.Grades = await _catalog.GetGradeLevelsAsync();
            ViewBag.Frameworks = await _catalog.GetFrameworksAsync();
        }
    }
}
