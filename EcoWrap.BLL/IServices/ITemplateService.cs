using EcoWrap.Models.Templates;

namespace EcoWrap.BLL.Services;

public interface ITemplateService
{
    Task<PagedResult<TemplateListItemDto>> ListAsync(TemplateQuery query, CancellationToken ct = default);
    Task<(TemplateDetailDto dto, string etag)?> GetDetailAsync(int id, CancellationToken ct = default);
}
