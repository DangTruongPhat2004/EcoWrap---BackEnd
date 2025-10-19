using EcoWrap.BLL.Validation;
using EcoWrap.Models.Designs;
using FluentValidation;
using System.Text.Json;

namespace EcoWrap.BLL.Validation;

public class CreateDesignValidator : AbstractValidator<CreateDesignDto>
{
    public CreateDesignValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);

        RuleFor(x => x.ThumbnailUrl)
            .Must(OptionalCloudinary).WithMessage("thumbnailUrl must be Cloudinary domain")
            .When(x => x.ThumbnailUrl != null);

        RuleFor(x => x.DielineSvgUrl)
            .Must(OptionalCloudinary).WithMessage("dielineSvgUrl must be Cloudinary domain")
            .When(x => x.DielineSvgUrl != null);

        RuleFor(x => x.DesignData)
            .NotEmpty()
            .Must(HasValidMeta).WithMessage("designData.meta (dpi 72..1200, size.width/height > 0) is required");

        RuleForEach(x => x.UsedAssets).SetValidator(new UsedAssetValidator());
    }

    private static bool OptionalCloudinary(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return false;
        return u.Host.Contains("res.cloudinary.com", StringComparison.OrdinalIgnoreCase);
    }

    // Kiểm tra nhẹ cấu trúc meta (không parse hết Fabric JSON)
    private static bool HasValidMeta(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("meta", out var meta)) return false;

            if (meta.TryGetProperty("dpi", out var dpiProp))
            {
                if (dpiProp.ValueKind != JsonValueKind.Number) return false;
                var dpi = dpiProp.GetDouble();
                if (dpi < 72 || dpi > 1200) return false;
            }
            else return false;

            if (meta.TryGetProperty("size", out var size))
            {
                if (!size.TryGetProperty("width", out var w) || w.GetDouble() <= 0) return false;
                if (!size.TryGetProperty("height", out var h) || h.GetDouble() <= 0) return false;
            }
            else return false;

            return true;
        }
        catch { return false; }
    }
}
