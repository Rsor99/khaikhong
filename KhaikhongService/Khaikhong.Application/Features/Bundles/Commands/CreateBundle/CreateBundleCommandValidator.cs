using FluentValidation;

namespace Khaikhong.Application.Features.Bundles.Commands.CreateBundle;

public sealed class CreateBundleCommandValidator : AbstractValidator<CreateBundleCommand>
{
    public CreateBundleCommandValidator()
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(new BundleRequestValidator());
    }
}
