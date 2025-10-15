// Validators/MyAssetsQueryValidator.cs
using FluentValidation;
public class MyAssetsQueryValidator : AbstractValidator<MyAssetsQueryDto>
{
    public MyAssetsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Type).Must(t => string.IsNullOrEmpty(t) || t is "image" or "raw")
            .WithMessage("Type must be 'image' or 'raw'");
    }
}
