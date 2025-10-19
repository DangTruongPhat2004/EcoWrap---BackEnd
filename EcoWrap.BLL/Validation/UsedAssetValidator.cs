using EcoWrap.Models.Designs;
using FluentValidation;

namespace EcoWrap.BLL.Validation;

public class UsedAssetValidator : AbstractValidator<UsedAssetDto>
{
    private static readonly string[] SourceEnum = { "user", "library", "template" };
    private static readonly string[] KindEnum = { "image", "svg", "pattern", "font" };

    public UsedAssetValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty()
            .Must(IsCloudinaryUrl).WithMessage("url must belong to Cloudinary (res.cloudinary.com)");

        RuleFor(x => x.Source)
            .NotEmpty()
            .Must(v => SourceEnum.Contains(v));

        RuleFor(x => x.Kind)
            .NotEmpty()
            .Must(v => KindEnum.Contains(v));

        RuleFor(x => x.Width).GreaterThan(0).When(x => x.Width.HasValue);
        RuleFor(x => x.Height).GreaterThan(0).When(x => x.Height.HasValue);
    }

    private static bool IsCloudinaryUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return false;
        return u.Host.Contains("res.cloudinary.com", StringComparison.OrdinalIgnoreCase);
    }
}
