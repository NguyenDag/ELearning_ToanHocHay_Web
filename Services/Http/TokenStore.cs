using Microsoft.AspNetCore.Http;

namespace ToanHocHay.WebApp.Services.Http
{
    /// <summary>
    /// Nơi đọc/ghi cặp token (access + refresh) của phiên hiện tại.
    /// Hiện lưu trong <c>Session</c>; nếu sau này chạy nhiều instance cần đổi sang
    /// distributed cache thật thì chỉ sửa ở đây.
    /// </summary>
    public interface ITokenStore
    {
        string? AccessToken { get; }
        string? RefreshToken { get; }
        DateTime? AccessTokenExpiresAtUtc { get; }

        void Save(string accessToken, DateTime accessExpiresUtc, string? refreshToken, DateTime? refreshExpiresUtc);
        void UpdateAccess(string accessToken, DateTime accessExpiresUtc, string? refreshToken, DateTime? refreshExpiresUtc);
        void Clear();
    }

    public class SessionTokenStore : ITokenStore
    {
        public const string KeyAccess = "Token";
        public const string KeyAccessAlias = "JWT";
        public const string KeyRefresh = "RefreshToken";
        public const string KeyAccessExp = "TokenExpiresAtUtc";
        public const string KeyRefreshExp = "RefreshTokenExpiresAtUtc";

        private readonly IHttpContextAccessor _http;

        public SessionTokenStore(IHttpContextAccessor http) => _http = http;

        private ISession? Session => _http.HttpContext?.Session;

        public string? AccessToken =>
            Session?.GetString(KeyAccess) ?? Session?.GetString(KeyAccessAlias);

        public string? RefreshToken => Session?.GetString(KeyRefresh);

        public DateTime? AccessTokenExpiresAtUtc
        {
            get
            {
                var raw = Session?.GetString(KeyAccessExp);
                return DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt)
                    ? dt
                    : null;
            }
        }

        public void Save(string accessToken, DateTime accessExpiresUtc, string? refreshToken, DateTime? refreshExpiresUtc)
        {
            var s = Session;
            if (s == null) return;

            s.SetString(KeyAccess, accessToken);
            s.SetString(KeyAccessAlias, accessToken);
            s.SetString(KeyAccessExp, accessExpiresUtc.ToUniversalTime().ToString("o"));

            if (!string.IsNullOrEmpty(refreshToken))
                s.SetString(KeyRefresh, refreshToken);
            if (refreshExpiresUtc.HasValue)
                s.SetString(KeyRefreshExp, refreshExpiresUtc.Value.ToUniversalTime().ToString("o"));
        }

        public void UpdateAccess(string accessToken, DateTime accessExpiresUtc, string? refreshToken, DateTime? refreshExpiresUtc)
            => Save(accessToken, accessExpiresUtc, refreshToken, refreshExpiresUtc);

        public void Clear()
        {
            var s = Session;
            if (s == null) return;
            s.Remove(KeyAccess);
            s.Remove(KeyAccessAlias);
            s.Remove(KeyRefresh);
            s.Remove(KeyAccessExp);
            s.Remove(KeyRefreshExp);
        }
    }
}
