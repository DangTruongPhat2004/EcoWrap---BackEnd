using EcoWrap.Models.Templates;
using  FluentValidation;

namespace EcoWrap.BLL.Validation;

public class AdminCreateTemplateValidator : AbstractValidator<AdminCreateTemplateDto>
{
    public AdminCreateTemplateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category != null);
        RuleFor(x => x.ThumbnailUrl).MaximumLength(500).When(x => x.ThumbnailUrl != null);

        RuleFor(x => x.DimensionsUnit).NotEmpty().MaximumLength(10);
        RuleFor(x => x.DimensionsWidth).GreaterThan(0);
        RuleFor(x => x.DimensionsHeight).GreaterThan(0);

        RuleFor(x => x.DefaultBleed).GreaterThanOrEqualTo(0).LessThanOrEqualTo(10);
        RuleFor(x => x.DefaultSafeArea).GreaterThanOrEqualTo(0).LessThanOrEqualTo(10);
    }
}
