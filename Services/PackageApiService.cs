using System.Net.Http.Headers;
using System.Text.Json;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services
{
    public class PackageApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions;

        public PackageApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _jsonOptions = ApiJson.Options;
        }

        private void AddAuthHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("Token");
            if (!string.IsNullOrEmpty(token))
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<List<PackageDto>> GetAllPackagesAsync()
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetAsync($"{ApiConstant.apiBaseUrl}/api/Package");
                if (!response.IsSuccessStatusCode) return new List<PackageDto>();
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<List<PackageDto>>>(json, _jsonOptions);
                return result?.Data ?? new List<PackageDto>();
            }
            catch { return new List<PackageDto>(); }
        }

        public async Task<PackageDto?> GetPackageByIdAsync(int id)
        {
            try
            {
                AddAuthHeader();
                var response = await _httpClient.GetAsync($"{ApiConstant.apiBaseUrl}/api/Package/{id}");
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<PackageDto>>(json, _jsonOptions);
                return result?.Data;
            }
            catch { return null; }
        }
    }
}