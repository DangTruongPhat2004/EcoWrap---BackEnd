using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("admin/assets")]
public class AdminAssetsController : ControllerBase
{
    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromServices] AssetSyncService svc, CancellationToken ct)
    {
        var res = await svc.SyncAsync(ct);
        return Ok(new { message = "Synced from Cloudinary", res.Created, res.Updated, res.Scanned });
    }
}
