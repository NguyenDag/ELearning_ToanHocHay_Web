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
            public static string ById(int id) => $"users/{id}";
            public static string ByEmail(string email) => $"users/email/{email}";
            public static string UpdateProfile(int id) => $"users/update-profile/{id}";
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

        public static class Courses
        {
            public const string List = "courses";
            public static string ById(int id) => $"courses/{id}";
            public static string BySlug(string slug) => $"courses/by-slug/{slug}";
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
            public static string ById(int id) => $"payments/{id}";
        }

        public static class Packages
        {
            public const string List = "packages";
            public static string ById(int id) => $"packages/{id}";
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
