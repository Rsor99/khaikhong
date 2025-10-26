using FluentValidation;

namespace Khaikhong.Application.Features.Bundles.Commands.UpdateBundle;

public sealed class UpdateBundleCommandValidator : AbstractValidator<UpdateBundleCommand>
{
    public UpdateBundleCommandValidator()
    {
        RuleFor(command => command.BundleId)
            .NotEmpty();

        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(new BundleRequestValidator());
    }
}
