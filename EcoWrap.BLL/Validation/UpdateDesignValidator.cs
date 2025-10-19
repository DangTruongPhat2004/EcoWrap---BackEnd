using EcoWrap.BLL.Validation;
using EcoWrap.Models.Designs;
using FluentValidation;

namespace EcoWrap.BLL.Validation;

public class UpdateDesignValidator : AbstractValidator<UpdateDesignDto>
{
    public UpdateDesignValidator()
    {
        RuleFor(x => x.Title).MaximumLength(200).When(x => x.Title != null);

        RuleFor(x => x.Status)
            .Must(s => new[] { "draft", "active", "archived" }.Contains(s!))
            .When(x => x.Status != null);

        RuleFor(x => x.ThumbnailUrl)
            .Must(OptionalCloudinary).WithMessage("thumbnailUrl must be Cloudinary domain")
            .When(x => x.ThumbnailUrl != null);

        RuleFor(x => x.DielineSvgUrl)
            .Must(OptionalCloudinary).WithMessage("dielineSvgUrl must be Cloudinary domain")
            .When(x => x.DielineSvgUrl != null);

        // Nếu có DesignData mới, chỉ cần not empty (scene lớn → không soi chi tiết)
        When(x => x.DesignData != null, () =>
        {
            RuleFor(x => x.DesignData!).NotEmpty();
        });

        RuleForEach(x => x.UsedAssets).SetValidator(new UsedAssetValidator());
    }

    private static bool OptionalCloudinary(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return false;
        return u.Host.Contains("res.cloudinary.com", StringComparison.OrdinalIgnoreCase);
    }
}
