using FluentValidation;
using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Configuration.Validation;

public class RemoteTaskExecutorValidator: AbstractValidator<IRemoteTaskExecutorConfiguration>
{
    public RemoteTaskExecutorValidator()
    {
        RuleFor(rte => rte.Url)
            .NotEmpty()
            .WithMessage(ValidationResources.RteUrlEmpty);

        RuleFor(rte => rte.ApplicationId)
            .NotEmpty()
            .WithMessage(ValidationResources.RteApplicationIdEmpty);
    }
}