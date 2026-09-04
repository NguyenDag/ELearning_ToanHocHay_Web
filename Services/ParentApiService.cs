using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs.Parent;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services
{
    /// <summary>
    /// Liên kết phụ huynh ⇄ học sinh (P6). Route <c>api/parents</c>. Mô hình mới:
    /// <c>ParentLink</c> (Pending → Active → Revoked) + <c>ParentInvite</c>.
    /// Revoke → mất quyền xem dashboard con ngay (403).
    /// </summary>
    public class ParentApiService
    {
        private readonly ApiClient _api;

        public ParentApiService(ApiClient api) => _api = api;

        public Task<ApiResult<ParentInfoDto>> GetInfoAsync(int parentId)
            => _api.GetAsync<ParentInfoDto>(ApiRoutes.Parents.ById(parentId));

        public Task<ApiResult<List<ParentLinkDto>>> GetChildrenAsync(int parentId)
            => _api.GetAsync<List<ParentLinkDto>>(ApiRoutes.Parents.Children(parentId));

        public Task<ApiResult<List<ChildOverviewDto>>> GetOverviewAsync(int parentId)
            => _api.GetAsync<List<ChildOverviewDto>>(ApiRoutes.Parents.ChildrenOverview(parentId));

        public Task<ApiResult<ParentInviteDto>> CreateInviteAsync(int parentId, CreateParentInviteDto dto)
            => _api.PostAsync<ParentInviteDto>(ApiRoutes.Parents.Invites(parentId), dto);

        public Task<ApiResult> RevokeChildAsync(int parentId, int studentId)
            => _api.DeleteAsync(ApiRoutes.Parents.RevokeChild(parentId, studentId));

        /// <summary>Học sinh nhập mã (ConnectionCode của phụ huynh hoặc token lời mời).</summary>
        public Task<ApiResult<ParentLinkDto>> LinkByCodeAsync(LinkParentInputDto dto)
            => _api.PostAsync<ParentLinkDto>(ApiRoutes.Parents.Link, dto);
    }
}
