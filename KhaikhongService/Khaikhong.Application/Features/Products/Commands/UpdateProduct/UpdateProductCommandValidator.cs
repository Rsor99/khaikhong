using FluentValidation;
using Khaikhong.Application.Features.Products.Dtos;

namespace Khaikhong.Application.Features.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .NotEmpty();

        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(new UpdateProductRequestValidator());
    }

    private sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequestDto>
    {
        public UpdateProductRequestValidator()
        {
            RuleFor(request => request.ProductId)
                .NotEmpty();

            RuleFor(request => request.Name)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(request => request.BasePrice)
                .GreaterThanOrEqualTo(0);

            RuleFor(request => request.BaseStock)
                .GreaterThanOrEqualTo(0)
                .When(request => request.BaseStock.HasValue);

            RuleFor(request => request.Options)
                .Must(HaveUniqueOptionNames)
                .When(request => request.Options is { Count: > 0 })
                .WithMessage("Option names must be unique.");

            When(request => request.Options is { Count: > 0 }, () =>
            {
                RuleForEach(request => request.Options).ChildRules(option =>
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

            RuleFor(request => request.Variants)
                .Must((request, variants) => variants is null
                                             || variants.Count == 0
                                             || variants.All(variant => VariantSelectionsCoverOptions(request.Options, variant.Selections)))
                .WithMessage("Each variant must include selections for all options.");

            When(request => request.Variants is { Count: > 0 }, () =>
            {
                RuleForEach(request => request.Variants).ChildRules(variant =>
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
}
