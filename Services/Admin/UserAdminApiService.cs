using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Models.DTOs.Payment;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services.Admin
{
    /// <summary>Gọi các API quản trị người dùng + phân quyền (SystemAdmin).</summary>
    public class UserAdminApiService
    {
        private readonly ApiClient _api;

        public UserAdminApiService(ApiClient api) => _api = api;

        public async Task<UsersIndexVm> ListAsync(AdminUserFilter f)
        {
            var qs = new List<string>
            {
                $"page={f.Page}",
                $"pageSize={f.PageSize}"
            };
            if (!string.IsNullOrWhiteSpace(f.Search)) qs.Add($"search={Uri.EscapeDataString(f.Search.Trim())}");
            if (f.UserType.HasValue) qs.Add($"userType={f.UserType.Value}");
            if (f.IsActive.HasValue) qs.Add($"isActive={f.IsActive.Value.ToString().ToLowerInvariant()}");
            if (f.Locked.HasValue) qs.Add($"locked={f.Locked.Value.ToString().ToLowerInvariant()}");
            if (f.EmailConfirmed.HasValue) qs.Add($"emailConfirmed={f.EmailConfirmed.Value.ToString().ToLowerInvariant()}");

            var r = await _api.GetAsync<PagedResultDto<AdminUserDto>>($"{ApiRoutes.Users.List}?{string.Join('&', qs)}");
            var data = r.Data ?? new PagedResultDto<AdminUserDto>();
            return new UsersIndexVm
            {
                Filter = f,
                Items = data.Items,
                Total = data.Total,
                Page = data.Page == 0 ? f.Page : data.Page,
                PageSize = data.PageSize == 0 ? f.PageSize : data.PageSize
            };
        }

        public async Task<AdminUserDto?> GetAsync(int id)
        {
            var r = await _api.GetAsync<AdminUserDto>(ApiRoutes.Users.ById(id));
            return r.IsSuccess ? r.Data : null;
        }

        public async Task<AdminUserDto?> GetByEmailForRoleAsync(string email)
        {
            var r = await _api.GetAsync<AdminUserDto>(ApiRoutes.Users.ByEmail(Uri.EscapeDataString(email)));
            return r.IsSuccess ? r.Data : null;
        }

        public Task<ApiResult<AdminUserDto>> CreateStaffAsync(CreateStaffUserVm vm)
            => _api.PostAsync<AdminUserDto>(ApiRoutes.Users.List, new
            {
                vm.Email,
                vm.Password,
                vm.FullName,
                vm.Phone,
                UserType = vm.UserType
            });

        public Task<ApiResult> LockAsync(int id, string reason)
            => _api.PostAsync(ApiRoutes.Admin.LockUser(id), new { Reason = reason });

        public Task<ApiResult> UnlockAsync(int id)
            => _api.PostAsync(ApiRoutes.Admin.UnlockUser(id));

        public Task<ApiResult> ChangeRoleAsync(int id, UserType newRole)
            => _api.PostAsync(ApiRoutes.Admin.ChangeRole(id), new { NewRole = newRole });

        public Task<ApiResult> ResetPasswordAsync(int id, string newPassword)
            => _api.PostAsync(ApiRoutes.Admin.ResetPassword(id), new { NewPassword = newPassword });

        public Task<ApiResult> ConfirmEmailAsync(int id)
            => _api.PostAsync(ApiRoutes.Admin.ConfirmEmail(id));

        public Task<ApiResult> DeactivateAsync(int id)
            => _api.PostAsync(ApiRoutes.Admin.DeactivateUser(id));

        public Task<ApiResult> ActivateAsync(int id)
            => _api.PostAsync(ApiRoutes.Admin.ActivateUser(id));

        public Task<ApiResult> DeleteAsync(int id)
            => _api.DeleteAsync(ApiRoutes.Users.ById(id));

        public async Task<List<RoleCapabilityDto>> GetRoleCapabilitiesAsync()
        {
            var r = await _api.GetAsync<List<RoleCapabilityDto>>(ApiRoutes.Admin.Roles);
            return r.IsSuccess && r.Data != null ? r.Data : new List<RoleCapabilityDto>();
        }
    }
}
