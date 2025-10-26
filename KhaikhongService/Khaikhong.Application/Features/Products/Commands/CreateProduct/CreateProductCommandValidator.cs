using FluentValidation;
using Khaikhong.Application.Features.Products.Dtos;

namespace Khaikhong.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Request).NotNull();

        RuleFor(command => command.Request.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(command => command.Request.BasePrice)
            .GreaterThanOrEqualTo(0);

        RuleFor(command => command.Request.Options)
            .Must(HaveUniqueOptionNames)
            .When(command => command.Request.Options is { Count: > 0 })
            .WithMessage("Option names must be unique.");

        When(command => command.Request.Options is { Count: > 0 }, () =>
        {
            RuleForEach(command => command.Request.Options).ChildRules(option =>
            {
                option.RuleFor(o => o.Name)
                    .NotEmpty()
                    .MaximumLength(100);

                option.RuleFor(o => o.Values)
                    .NotEmpty()
                    .WithMessage("Each option must contain at least one value.")
                    .Must(values => values.Distinct(StringComparer.OrdinalIgnoreCase).Count() == values.Count)
                    .WithMessage("Option values must be unique.");
            });
        });

        RuleFor(command => command.Request.Variants)
            .Must((command, variants) => variants is null
                                         || variants.Count == 0
                                         || variants.All(variant => VariantSelectionsCoverOptions(command.Request.Options, variant.Selections)))
            .WithMessage("Each variant must include selections for all options.");

        When(command => command.Request.Variants is { Count: > 0 }, () =>
        {
            RuleForEach(command => command.Request.Variants).ChildRules(variant =>
            {
                variant.RuleFor(v => v.Price)
                    .GreaterThanOrEqualTo(0);

                variant.RuleFor(v => v.Stock)
                    .GreaterThanOrEqualTo(0);

                variant.RuleFor(v => v.Selections)
                    .NotEmpty()
                    .WithMessage("Variant selections are required.");

                variant.RuleForEach(v => v.Selections).ChildRules(selection =>
                {
                    selection.RuleFor(s => s.OptionName).NotEmpty();
                    selection.RuleFor(s => s.Value).NotEmpty();
                });
            });
        });
    }

    private static bool HaveUniqueOptionNames(IReadOnlyCollection<ProductOptionDto> options)
    {
        if (options.Count == 0)
        {
            return true;
        }

        return options
            .Select(option => option.Name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == options.Count;
    }

    private static bool VariantSelectionsCoverOptions(
        IReadOnlyCollection<ProductOptionDto> options,
        IReadOnlyCollection<VariantSelectionDto>? selections)
    {
        if (options.Count == 0)
        {
            return true;
        }

        IReadOnlyCollection<VariantSelectionDto> safeSelections = selections ?? Array.Empty<VariantSelectionDto>();

        HashSet<string> optionNames = options
            .Select(option => option.Name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        HashSet<string> selectionNames = safeSelections
            .Select(selection => selection.OptionName.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return optionNames.SetEquals(selectionNames);
    }
}
