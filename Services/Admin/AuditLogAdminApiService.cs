using System.Globalization;
using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Models.DTOs.Payment;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services.Admin
{
    public class AuditLogAdminApiService
    {
        private readonly ApiClient _api;

        public AuditLogAdminApiService(ApiClient api) => _api = api;

        private static string BuildQuery(AuditLogFilterVm f)
        {
            var qs = new List<string> { $"page={f.Page}", $"pageSize={f.PageSize}" };
            if (!string.IsNullOrWhiteSpace(f.EntityType)) qs.Add($"entityType={Uri.EscapeDataString(f.EntityType)}");
            if (!string.IsNullOrWhiteSpace(f.Action)) qs.Add($"action={Uri.EscapeDataString(f.Action)}");
            if (f.UserId.HasValue) qs.Add($"userId={f.UserId}");
            if (!string.IsNullOrWhiteSpace(f.Q)) qs.Add($"q={Uri.EscapeDataString(f.Q.Trim())}");
            if (f.From.HasValue) qs.Add($"fromUtc={Uri.EscapeDataString(f.From.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture))}");
            if (f.To.HasValue) qs.Add($"toUtc={Uri.EscapeDataString(f.To.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture))}");
            return string.Join('&', qs);
        }

        public async Task<AuditLogIndexVm> ListAsync(AuditLogFilterVm f)
        {
            var r = await _api.GetAsync<PagedResultDto<AuditLogItemDto>>($"{ApiRoutes.Admin.AuditLogs}?{BuildQuery(f)}");
            var facets = await _api.GetAsync<AuditLogFacetsDto>(ApiRoutes.Admin.AuditLogsFacets);
            var data = r.Data ?? new PagedResultDto<AuditLogItemDto>();
            return new AuditLogIndexVm
            {
                Filter = f,
                Items = data.Items,
                Total = data.Total,
                Page = data.Page == 0 ? f.Page : data.Page,
                PageSize = data.PageSize == 0 ? f.PageSize : data.PageSize,
                Facets = facets.Data ?? new AuditLogFacetsDto()
            };
        }

        public async Task<(byte[] Bytes, string FileName)?> ExportCsvAsync(AuditLogFilterVm f)
        {
            var resp = await _api.Raw.GetAsync($"{ApiRoutes.Admin.AuditLogsExport}?{BuildQuery(f)}");
            if (!resp.IsSuccessStatusCode) return null;
            var bytes = await resp.Content.ReadAsByteArrayAsync();
            var name = resp.Content.Headers.ContentDisposition?.FileNameStar
                       ?? resp.Content.Headers.ContentDisposition?.FileName
                       ?? $"nhat-ky-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            return (bytes, name.Trim('"'));
        }
    }
}
