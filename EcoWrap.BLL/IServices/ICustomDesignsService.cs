using EcoWrap.Models.Designs;

namespace EcoWrap.BLL.IServices;

public interface ICustomDesignsService
{
    Task<(IReadOnlyList<DesignListItemDto> items, int total)> GetListAsync(
        long ownerId, int page, int pageSize, string? q, string? status, CancellationToken ct);

    Task<(DesignDetailDto dto, byte[] rowVersion)?> GetAsync(long id, long ownerId, CancellationToken ct);

    Task<(DesignDetailDto dto, byte[] rowVersion)> CreateAsync(
        long ownerId, CreateDesignDto body, CancellationToken ct);

    /// <summary>
    /// ifMatch: RowVersion từ ETag (null => không cho cập nhật, controller sẽ chặn trước)
    /// </summary>
    Task<(DesignDetailDto dto, byte[] rowVersion)?> UpdateAsync(
        long id, long ownerId, byte[] ifMatch, UpdateDesignDto body, CancellationToken ct);

    Task<bool> DeleteAsync(long id, long ownerId, bool hard, CancellationToken ct);
}
