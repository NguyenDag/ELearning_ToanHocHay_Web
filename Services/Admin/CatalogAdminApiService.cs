using System.Text.Json;
using ToanHocHay.WebApp.Areas.Admin.Models;
using ToanHocHay.WebApp.Common.Constants;
using ToanHocHay.WebApp.Services.Http;

namespace ToanHocHay.WebApp.Services.Admin
{
    /// <summary>Danh mục (môn / lớp / bộ sách) cho các form quản trị.</summary>
    public class CatalogAdminApiService
    {
        private readonly ApiClient _api;

        public CatalogAdminApiService(ApiClient api) => _api = api;

        public async Task<List<CatalogRefDto>> GetSubjectsAsync(bool includeInactive = true)
        {
            var r = await _api.GetAsync<List<JsonElement>>($"{ApiRoutes.Catalog.Subjects}?includeInactive={includeInactive.ToString().ToLowerInvariant()}");
            return Map(r.Data, "subjectId", "name");
        }

        public async Task<List<CatalogRefDto>> GetGradeLevelsAsync(bool includeInactive = true)
        {
            var r = await _api.GetAsync<List<JsonElement>>($"{ApiRoutes.Catalog.GradeLevels}?includeInactive={includeInactive.ToString().ToLowerInvariant()}");
            return Map(r.Data, "gradeLevelId", "name");
        }

        public async Task<List<CatalogRefDto>> GetFrameworksAsync(bool includeInactive = true)
        {
            var r = await _api.GetAsync<List<JsonElement>>($"{ApiRoutes.Catalog.Frameworks}?includeInactive={includeInactive.ToString().ToLowerInvariant()}");
            return Map(r.Data, "frameworkId", "name");
        }

        private static List<CatalogRefDto> Map(List<JsonElement>? rows, string idProp, string nameProp)
        {
            var list = new List<CatalogRefDto>();
            if (rows == null) return list;
            foreach (var el in rows)
            {
                if (el.ValueKind != JsonValueKind.Object) continue;
                var id = GetInt(el, idProp);
                var name = GetString(el, nameProp) ?? $"#{id}";
                list.Add(new CatalogRefDto { Id = id, Name = name });
            }
            return list;
        }

        private static int GetInt(JsonElement el, string prop)
        {
            foreach (var p in new[] { prop, Cap(prop) })
                if (el.TryGetProperty(p, out var v) && v.TryGetInt32(out var i)) return i;
            return 0;
        }

        private static string? GetString(JsonElement el, string prop)
        {
            foreach (var p in new[] { prop, Cap(prop) })
                if (el.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String) return v.GetString();
            return null;
        }

        private static string Cap(string s) => char.ToUpperInvariant(s[0]) + s[1..];
    }
}
