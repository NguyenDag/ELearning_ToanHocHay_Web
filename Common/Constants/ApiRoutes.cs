namespace ToanHocHay.WebApp.Common.Constants
{
    /// <summary>
    /// Tập trung toàn bộ đường dẫn tới Backend API.
    /// Backend đã chuẩn hoá sang kebab-case số nhiều (A5) — mọi route cũ dạng
    /// <c>/api/User</c>, <c>/api/Subscription</c>, <c>/api/ExerciseAttempts</c>... đã đổi.
    ///
    /// Các hằng ở đây là đường dẫn TƯƠNG ĐỐI (không có <c>/</c> đầu) để dùng với
    /// <see cref="ToanHocHay.WebApp.Services.Http.ApiClient"/> có <c>BaseAddress = {apiBaseUrl}/api/</c>.
    /// </summary>
    public static class ApiRoutes
    {
        // ---------- auth (giữ nguyên tiền tố api/auth) ----------
        public static class Auth
        {
            public const string Login = "auth/login";
            public const string Register = "auth/register";
            public const string RefreshToken = "auth/refresh-token";
            public const string Logout = "auth/logout";
            public const string Me = "auth/me";
            public const string ChangePassword = "auth/change-password";           // userId lấy từ token
            public const string ConfirmEmail = "auth/confirm-email";               // ?token=
            public const string ResendConfirmation = "auth/resend-confirmation";   // body { email }
            public const string ForgotPassword = "auth/forgot-password";           // body { email }
            public const string ResetPassword = "auth/reset-password";             // body { token, newPassword }
        }

        // ---------- users ----------
        public static class Users
        {
            public const string List = "users";                                     // GET (admin, có filter) / POST tạo staff
            public static string ById(int id) => $"users/{id}";
            public static string ByEmail(string email) => $"users/email/{email}";
            public static string UpdateProfile(int id) => $"users/update-profile/{id}";
        }

        // ---------- admin (SystemAdmin) ----------
        public static class Admin
        {
            public const string Roles = "admin/roles";
            public const string AuditLogs = "admin/audit-logs";
            public const string AuditLogsExport = "admin/audit-logs/export";
            public const string AuditLogsFacets = "admin/audit-logs/facets";
            public const string Config = "admin/config";                            // ?group=
            public static string ConfigKey(string key) => $"admin/config/{key}";
            public static string LockUser(int id) => $"admin/users/{id}/lock";
            public static string UnlockUser(int id) => $"admin/users/{id}/unlock";
            public static string ChangeRole(int id) => $"admin/users/{id}/role";
            public static string ResetPassword(int id) => $"admin/users/{id}/reset-password";
            public static string ConfirmEmail(int id) => $"admin/users/{id}/confirm-email";
            public static string DeactivateUser(int id) => $"admin/users/{id}/deactivate";
            public static string ActivateUser(int id) => $"admin/users/{id}/activate";
        }

        // ---------- catalog / courses / learn / enrollments / progress ----------
        public static class Catalog
        {
            public const string Subjects = "catalog/subjects";
            public const string GradeLevels = "catalog/grade-levels";
            public const string Frameworks = "catalog/frameworks";
            public static string Subject(int id) => $"catalog/subjects/{id}";
            public static string GradeLevel(int id) => $"catalog/grade-levels/{id}";
            public static string Framework(int id) => $"catalog/frameworks/{id}";
        }

        // ---------- question banks + questions (authoring) ----------
        public static class QuestionBanks
        {
            public const string List = "question-banks";                            // ?subjectId=&gradeLevelId=&includeInactive=
            public static string ById(int bankId) => $"question-banks/{bankId}";
            public static string Questions(int bankId) => $"question-banks/{bankId}/questions"; // ?status=&search=&page=&pageSize=
            public static string Question(int questionId) => $"question-banks/questions/{questionId}";
            public static string SubmitQuestion(int questionId) => $"question-banks/questions/{questionId}/submit";
            public static string ReviewQuestion(int questionId) => $"question-banks/questions/{questionId}/review";
            public const string CreateQuestions = "questions";                       // POST list<CreateQuestionDto>
        }

        // ---------- exercises (authoring) ----------
        public static class ExercisesAdmin
        {
            public const string List = "exercises";
            public static string ById(int id) => $"exercises/{id}";
            public static string ForEdit(int id) => $"exercises/{id}/for-edit";
            public static string Questions(int id) => $"exercises/{id}/questions";
            public static string Publish(int id) => $"exercises/{id}/publish";
            public static string Unpublish(int id) => $"exercises/{id}/unpublish";
            public static string Question(int exerciseId, int questionId) => $"exercises/{exerciseId}/questions/{questionId}";
        }

        public static class Courses
        {
            public const string List = "courses";
            public static string ById(int id) => $"courses/{id}";
            public static string BySlug(string slug) => $"courses/by-slug/{slug}";

            // authoring / workflow
            public static string Archive(int id) => $"courses/{id}/archive";
            public static string Unarchive(int id) => $"courses/{id}/unarchive";
            public static string Versions(int courseId) => $"courses/{courseId}/versions";
            public static string SubmitVersion(int versionId) => $"courses/versions/{versionId}/submit";
            public static string ReviewVersion(int versionId) => $"courses/versions/{versionId}/review";
            public static string PublishVersion(int versionId) => $"courses/versions/{versionId}/publish";
            public static string ArchiveVersion(int versionId) => $"courses/versions/{versionId}/archive";
            public static string VersionReviews(int versionId) => $"courses/versions/{versionId}/reviews";
            public static string ResolveComment(int commentId) => $"courses/reviews/comments/{commentId}/resolve";
        }

        // ---------- content authoring (cây nội dung + block) ----------
        public static class ContentAuthoring
        {
            public static string Tree(int versionId) => $"content/versions/{versionId}/tree";
            public static string Node(int nodeId) => $"content/nodes/{nodeId}";
            public static string CreateNode(int versionId) => $"content/versions/{versionId}/nodes";
            public static string Reorder(int versionId, int? parentNodeId) =>
                $"content/versions/{versionId}/nodes/reorder" + (parentNodeId.HasValue ? $"?parentNodeId={parentNodeId}" : "");
            public static string MoveNode(int nodeId) => $"content/nodes/{nodeId}/move";
            public static string Blocks(int nodeId) => $"content/nodes/{nodeId}/blocks";
            public static string Block(int blockId) => $"content/blocks/{blockId}";
            public static string Resources(int nodeId) => $"content/nodes/{nodeId}/resources";
            public static string Resource(int resourceId) => $"content/resources/{resourceId}";
        }

        // ---------- import khung chương trình từ file CSV ----------
        public static class ContentImport
        {
            public static string Validate(int? versionId) =>
                "content/import/validate" + (versionId is int v ? $"?versionId={v}" : "");
            public const string Course = "content/import/course";
            public static string IntoVersion(int versionId, bool replace) =>
                $"content/import/versions/{versionId}" + (replace ? "?replace=true" : "");
            public static string Jobs(int take) => $"content/import/jobs?take={take}";
            public static string Job(int id) => $"content/import/jobs/{id}";

            /// <summary>Import ngân hàng câu hỏi / đề độc lập. bankId → thêm vào; ngược lại subjectId+gradeLevelId tạo mới.</summary>
            public static string QuestionBank(int? bankId, int? subjectId, int? gradeLevelId, bool dryRun)
            {
                var q = new List<string>();
                if (bankId is > 0) q.Add($"bankId={bankId}");
                if (subjectId is > 0) q.Add($"subjectId={subjectId}");
                if (gradeLevelId is > 0) q.Add($"gradeLevelId={gradeLevelId}");
                if (dryRun) q.Add("dryRun=true");
                return "content/import/question-bank" + (q.Count > 0 ? "?" + string.Join('&', q) : "");
            }
        }

        public static class CatalogAuthoring
        {
            public const string Subjects = "catalog/subjects";
            public static string Subject(int id) => $"catalog/subjects/{id}";
            public const string GradeLevels = "catalog/grade-levels";
            public static string GradeLevel(int id) => $"catalog/grade-levels/{id}";
            public const string Frameworks = "catalog/frameworks";
            public static string Framework(int id) => $"catalog/frameworks/{id}";
        }

        public static class Learn
        {
            public static string CourseContent(int courseId) => $"learn/courses/{courseId}/content";
            public static string Node(int nodeId) => $"learn/nodes/{nodeId}";
        }

        public static class Enrollments
        {
            public const string Mine = "enrollments/me";
            public static string EnrollCourse(int courseId) => $"enrollments/courses/{courseId}";
        }

        public static class Progress
        {
            public static string CompleteLesson(int nodeId) => $"progress/lessons/{nodeId}/complete"; // body { secondsViewed }
            public static string Version(int courseVersionId) => $"progress/versions/{courseVersionId}";
            public static string Heatmap(int studentId, int days = 90) => $"progress/students/{studentId}/heatmap?days={days}";
        }

        // ---------- exercises / exercise-attempts ----------
        public static class Exercises
        {
            public const string List = "exercises";
            public static string ById(int id) => $"exercises/{id}";
            public static string ForEdit(int id) => $"exercises/{id}/for-edit";
            public static string Questions(int id) => $"exercises/{id}/questions";
        }

        public static class ExerciseAttempts
        {
            public const string Start = "exercise-attempts/start";
            public const string StartRandom = "exercise-attempts/start-random";
            public const string SaveAnswer = "exercise-attempts/save-answer";
            public const string Complete = "exercise-attempts/complete";
            public static string Result(int attemptId) => $"exercise-attempts/{attemptId}/result";
            public static string History(int studentId) => $"exercise-attempts/student/{studentId}/history";
            public static string ReportTabSwitch(int attemptId) => $"exercise-attempts/{attemptId}/report-tab-switch";
            public static string FeedbackStatus(int attemptId) => $"exercise-attempts/{attemptId}/feedback-status";
            public static string TabSwitchLogs(int attemptId) => $"exercise-attempts/{attemptId}/tab-switch-logs";
        }

        // ---------- ai-hints ----------
        public static class AiHints
        {
            public const string Create = "ai-hints";
            public const string Quota = "ai-hints/quota";
            public static string ByAttempt(int attemptId) => $"ai-hints/by-attempt/{attemptId}";
            public static string ByAttemptQuestion(int attemptId, int questionId) =>
                $"ai-hints/by-attempt-question?attemptId={attemptId}&questionId={questionId}";
        }

        // ---------- students / dashboard ----------
        public static class Students
        {
            public const string DashboardStats = "students/dashboard-stats";                        // từ token
            public static string DashboardOverview(int id) => $"students/{id}/dashboard/overview";
            public static string ChapterScoreComparison(int id) => $"students/{id}/dashboard/chapter-score-comparison";
            public static string AiAssessment(int id) => $"students/{id}/dashboard/ai-assessment";
            public static string AiRoadmap(int id) => $"students/{id}/dashboard/ai-roadmap";
            public static string CurrentSubscription(int id) => $"students/{id}/subscription/current";
        }

        // ---------- subscriptions / payments / packages ----------
        public static class Subscriptions
        {
            public const string Create = "subscriptions";                          // body { StudentId, PackageId }
            public const string Mine = "subscriptions/me";
            public static string ById(int id) => $"subscriptions/{id}";
            public static string Cancel(int id) => $"subscriptions/cancel/{id}";
            public static string Status(int id) => $"subscriptions/status/{id}";
            public static string CheckPremium(int studentId) => $"subscriptions/check-premium/{studentId}";
        }

        public static class Payments
        {
            public const string Mine = "payments/me";

            /// <summary>Toàn bộ giao dịch (Finance/Admin) — ?page=&pageSize=&status=&method=&from=&to=&search=</summary>
            public const string All = "payments";
            public static string ById(int id) => $"payments/{id}";
        }

        public static class Packages
        {
            public const string List = "packages";

            /// <summary>Danh sách quản lý gồm gói đã tắt + số thuê bao (Finance/Admin).</summary>
            public const string Manage = "packages/manage";
            public static string ById(int id) => $"packages/{id}";
        }

        // ---------- finance analytics (Finance/Admin) ----------
        public static class Finance
        {
            public const string AnalyticsSummary = "finance/analytics/summary";
            public const string RevenueSeries = "finance/analytics/revenue-series";
            public const string RevenueByPackage = "finance/analytics/by-package";
        }

        // ---------- parents ----------
        public static class Parents
        {
            public static string ById(int id) => $"parents/{id}";
            public static string Invites(int id) => $"parents/{id}/invites";
            public const string Link = "parents/link";                             // body LinkParentDto { Code, Relationship }
            public static string Children(int id) => $"parents/{id}/children";
            public static string ChildrenOverview(int id) => $"parents/{id}/children/overview";
            public static string RevokeChild(int id, int studentId) => $"parents/{id}/children/{studentId}";
        }

        // ---------- refunds ----------
        public static class Refunds
        {
            public const string Create = "refunds";
            public const string Mine = "refunds/me";
            public static string ById(int id) => $"refunds/{id}";
        }

        // ---------- notifications ----------
        public static class Notifications
        {
            public const string List = "notifications";
            public const string UnreadCount = "notifications/unread-count";
            public const string ReadAll = "notifications/read-all";
            public const string Preferences = "notifications/preferences";
            public static string Read(int id) => $"notifications/{id}/read";
        }

        // ---------- chatbot ----------
        public static class Chatbot
        {
            public const string Message = "chatbot/message";
            public const string QuickReply = "chatbot/quick-reply";
            public const string Trigger = "chatbot/trigger";
            public const string Conversations = "chatbot/conversations";
            public const string Health = "chatbot/health";
            public const string RequestHuman = "chatbot/request-human";
            public static string ConversationMessages(int id) => $"chatbot/conversations/{id}/messages";
            public static string CloseConversation(int id) => $"chatbot/conversations/{id}/close";
        }
    }
}
