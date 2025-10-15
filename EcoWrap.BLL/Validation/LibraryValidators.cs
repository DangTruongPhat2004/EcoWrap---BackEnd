using FluentValidation;

public class LibraryQueryValidator : AbstractValidator<LibraryQueryDto>
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    { "icon","icons","pattern","patterns","illustration","illustrations" };
    public LibraryQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        When(x => !string.IsNullOrWhiteSpace(x.Category), () =>
          RuleFor(x => x.Category!).Must(c => Allowed.Contains(c.Trim()))
            .WithMessage("category phải thuộc: icons|patterns|illustrations"));
        RuleFor(x => x.Search).MaximumLength(100);
    }

}
