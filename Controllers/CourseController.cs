using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Models.DTOs.Content;
using ToanHocHay.WebApp.Services;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Controllers
{
    /// <summary>
    /// Xem bài giảng — tầng nội dung mới (P2). Cho phép KHÁCH (chưa đăng nhập) duyệt cây khoá học
    /// và xem node miễn phí; ghi tiến độ / ghi danh thì cần đăng nhập.
    ///
    /// Backend trả cây <c>ContentNode</c> (Chapter → Topic/SubTopic → Lesson) — ở đây map lại sang
    /// <see cref="CurriculumDto"/> để tái dùng các view sẵn có.
    /// </summary>
    public class CourseController : Controller
    {
        private const string SessCourseId = "CurrentCourseId";
        private const string SessVersionId = "CurrentCourseVersionId";

        private readonly ContentApiService _content;
        private readonly ILogger<CourseController> _logger;

        public CourseController(ContentApiService content, ILogger<CourseController> logger)
        {
            _content = content;
            _logger = logger;
        }

        private int? CurrentStudentId =>
            int.TryParse(User.FindFirst("StudentId")?.Value, out var id) ? id : null;

        // ---------------------------------------------------------------------
        // Danh sách chương / bài của một khoá học
        // ---------------------------------------------------------------------
        public async Task<IActionResult> Index(int? id)
        {
            var courseId = id ?? HttpContext.Session.GetInt32(SessCourseId) ?? await ResolveDefaultCourseIdAsync();
            if (courseId is null or 0)
                return View("Index", new CurriculumDto());

            var res = await _content.GetCourseContentAsync(courseId.Value);
            if (res.IsTooManyRequests)
            {
                ViewBag.GuestLimited = true;
                this.ShowToast(
                    "Bạn đã dùng hết lượt xem thử dành cho khách. Đăng nhập để tiếp tục học nhé!",
                    "warning", "Đăng nhập", "/Account/Login");
                return View("Index", new CurriculumDto());
            }
            if (!res.IsSuccess || res.Data == null)
                return View("Index", new CurriculumDto());

            var course = res.Data;
            HttpContext.Session.SetInt32(SessCourseId, course.CourseId);
            HttpContext.Session.SetInt32(SessVersionId, course.CourseVersionId);

            ViewBag.CourseId = course.CourseId;
            ViewBag.IsEntitled = course.IsEntitled;
            ViewBag.AccessLevel = course.AccessLevel;          // "Full" | "FreeOnly"
            ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated == true;
            ViewBag.IsStudent = CurrentStudentId != null;
            ViewBag.CompletedLessonIds = await GetCompletedNodeIdsAsync(course.CourseVersionId);

            return View("Index", MapToCurriculum(course));
        }

        public Task<IActionResult> Chapter(int id) => Index(HttpContext.Session.GetInt32(SessCourseId));
        public Task<IActionResult> Topic(int id) => Index(HttpContext.Session.GetInt32(SessCourseId));

        // ---------------------------------------------------------------------
        // Xem một bài học
        // ---------------------------------------------------------------------
        public async Task<IActionResult> Learning(int id)
        {
            var res = await _content.GetNodeAsync(id);

            if (res.IsForbidden)
            {
                ViewBag.LockMessage = res.DisplayMessage;
                ViewBag.CourseId = HttpContext.Session.GetInt32(SessCourseId);
                ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated == true;
                return View("LessonLocked");
            }
            if (res.IsTooManyRequests)
            {
                this.PushToastWarning(
                    "Bạn đã xem hết số bài học thử dành cho khách. Vui lòng đăng nhập để tiếp tục.");
                return RedirectToAction("Login", "Account");
            }
            if (!res.IsSuccess || res.Data == null)
                return NotFound();

            var node = res.Data;
            var lesson = MapNodeDetailToLesson(node);

            // Cây khoá học cho sidebar + điều hướng (nếu biết khoá hiện tại).
            var courseId = HttpContext.Session.GetInt32(SessCourseId);
            CourseContentDto? course = null;
            if (courseId is > 0)
            {
                var cr = await _content.GetCourseContentAsync(courseId.Value);
                if (cr.IsSuccess) course = cr.Data;
            }

            if (course != null)
            {
                ViewBag.FullCurriculum = MapToCurriculum(course);
                ViewBag.RelatedLessons = FindSiblingLessons(course, node.NodeId);
                ViewBag.CompletedLessonIds = await GetCompletedNodeIdsAsync(course.CourseVersionId);
            }
            else
            {
                ViewBag.CompletedLessonIds = new List<int>();
            }

            return View("Lesson", lesson);
        }

        // ---------------------------------------------------------------------
        // Ghi tiến độ xem bài (JS gọi khi đủ thời gian xem)
        // ---------------------------------------------------------------------
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressRequest req)
        {
            if (req == null || req.LessonId <= 0)
                return BadRequest(new { ok = false });

            if (req.WatchTime < 20)
                return Ok(new { ok = false, message = "Cần xem bài ít nhất 20 giây để ghi nhận." });

            var r = await _content.MarkLessonCompleteAsync(req.LessonId, req.WatchTime);

            if (r.IsForbidden)
                return Ok(new { ok = false, needEnroll = true, message = "Hãy ghi danh khoá học để lưu tiến độ." });

            return r.IsSuccess
                ? Ok(new { ok = true })
                : Ok(new { ok = false, message = r.DisplayMessage });
        }

        // ---------------------------------------------------------------------
        // Ghi danh khoá học
        // ---------------------------------------------------------------------
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(int courseId)
        {
            if (CurrentStudentId == null)
            {
                this.PushToastError("Chỉ học sinh mới ghi danh được khoá học.");
                return RedirectToAction("Index", new { id = courseId });
            }

            var r = await _content.EnrollAsync(courseId);
            this.PushToastResult(r, "Ghi danh khoá học thành công!");

            return RedirectToAction("Index", new { id = courseId });
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------
        private async Task<int?> ResolveDefaultCourseIdAsync()
        {
            var r = await _content.GetCoursesAsync(publishedOnly: true);
            if (!r.IsSuccess || r.Data is not { Count: > 0 }) return null;
            return r.Data
                       .Where(c => c.PublishedVersionId != null)
                       .OrderBy(c => c.DisplayOrder)
                       .Select(c => (int?)c.CourseId)
                       .FirstOrDefault()
                   ?? r.Data[0].CourseId;
        }

        private async Task<List<int>> GetCompletedNodeIdsAsync(int courseVersionId)
        {
            if (CurrentStudentId == null || courseVersionId <= 0) return new();
            var r = await _content.GetVersionProgressAsync(courseVersionId);
            return r.IsSuccess && r.Data != null
                ? r.Data.Where(p => p.IsCompleted).Select(p => p.NodeId).ToList()
                : new();
        }

        private static CurriculumDto MapToCurriculum(CourseContentDto c)
        {
            var dto = new CurriculumDto
            {
                CurriculumId = c.CourseId,
                CurriculumName = c.Title,
                Description = c.Description,
                Chapters = new()
            };

            var order = 1;
            foreach (var chNode in c.Tree.Where(n => !n.IsHidden).OrderBy(n => n.OrderIndex))
            {
                var chapter = new ChapterDto
                {
                    ChapterId = chNode.NodeId,
                    ChapterName = chNode.Title,
                    OrderIndex = chNode.OrderIndex > 0 ? chNode.OrderIndex : order,
                    Topics = new()
                };
                order++;

                var kids = chNode.Children.Where(n => !n.IsHidden).OrderBy(n => n.OrderIndex).ToList();

                // Bài học nằm thẳng dưới chương → gom vào một "chủ đề" mang tên chương.
                var directLessons = kids.Where(n => n.NodeType == ContentNodeType.Lesson).ToList();
                if (directLessons.Count > 0)
                    chapter.Topics.Add(new TopicDto
                    {
                        TopicId = chNode.NodeId,
                        TopicName = chNode.Title,
                        OrderIndex = 0,
                        Lessons = directLessons.Select(MapLessonNode).ToList()
                    });

                foreach (var tNode in kids.Where(n => n.NodeType != ContentNodeType.Lesson))
                    chapter.Topics.Add(new TopicDto
                    {
                        TopicId = tNode.NodeId,
                        TopicName = tNode.Title,
                        OrderIndex = tNode.OrderIndex,
                        Lessons = CollectLessons(tNode)
                    });

                dto.Chapters.Add(chapter);
            }
            return dto;
        }

        private static List<LessonDto> CollectLessons(ContentNodeDto topic)
        {
            var list = new List<LessonDto>();
            void Walk(ContentNodeDto n)
            {
                foreach (var ch in n.Children.Where(x => !x.IsHidden).OrderBy(x => x.OrderIndex))
                {
                    if (ch.NodeType == ContentNodeType.Lesson) list.Add(MapLessonNode(ch));
                    else Walk(ch);
                }
            }
            Walk(topic);
            return list;
        }

        private static LessonDto MapLessonNode(ContentNodeDto n) => new()
        {
            LessonId = n.NodeId,
            TopicId = n.ParentNodeId ?? 0,
            LessonName = n.Title,
            DurationMinutes = n.DurationMinutes,
            OrderIndex = n.OrderIndex,
            IsFree = n.IsFree,
            IsActive = true,
            Status = LessonStatus.Published
        };

        private static LessonDto MapNodeDetailToLesson(ContentNodeDetailDto node)
        {
            var lesson = MapLessonNode(node);

            var contents = node.Blocks
                .OrderBy(b => b.OrderIndex)
                .Select(b => new LessonContentDto
                {
                    ContentId = b.BlockId,
                    BlockType = b.BlockType,
                    ContentText = b.ContentText,
                    ContentUrl = b.ContentUrl,
                    OrderIndex = b.OrderIndex
                })
                .ToList();

            // Tài liệu đính kèm → hiển thị như block Pdf ở cuối.
            var nextOrder = (contents.LastOrDefault()?.OrderIndex ?? 0) + 1;
            foreach (var r in node.Resources.Where(r => !string.IsNullOrEmpty(r.ExternalUrl)).OrderBy(r => r.OrderIndex))
            {
                contents.Add(new LessonContentDto
                {
                    ContentId = -r.ResourceId,
                    BlockType = LessonBlockType.Pdf,
                    ContentText = r.Title,
                    ContentUrl = r.ExternalUrl,
                    OrderIndex = nextOrder++
                });
            }

            lesson.Contents = contents;
            return lesson;
        }

        private static List<LessonDto> FindSiblingLessons(CourseContentDto course, int nodeId)
        {
            ContentNodeDto? parent = null;
            void Walk(ContentNodeDto n)
            {
                if (parent != null) return;
                if (n.Children.Any(c => c.NodeId == nodeId)) { parent = n; return; }
                foreach (var c in n.Children) Walk(c);
            }
            foreach (var root in course.Tree) Walk(root);

            var siblings = parent?.Children ?? course.Tree;
            return siblings
                .Where(n => n.NodeType == ContentNodeType.Lesson && !n.IsHidden)
                .OrderBy(n => n.OrderIndex)
                .Select(MapLessonNode)
                .ToList();
        }
    }

    public class UpdateProgressRequest
    {
        public int StudentId { get; set; }
        public int LessonId { get; set; }
        public int WatchTime { get; set; }
    }
}
