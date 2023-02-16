using FluentValidation;
using Nox.Cli.Abstractions.Configuration;

namespace Nox.Cli.Configuration.Validation;

public class CliAuthValidator: AbstractValidator<ICliAuthConfiguration>
{
    public CliAuthValidator()
    {
        RuleFor(auth => auth.provider)
            .NotEmpty()
            .WithMessage(auth => ValidationResources.AuthAuthorityEmpty);

        var providerConditions = new List<string>() { "azure", "aws", "google" };
        RuleFor(auth => auth.provider.ToLower())
            .Must(provider => providerConditions.Contains(provider.ToLower()))
            .WithMessage(auth => string.Format(ValidationResources.AuthProviderInvalid, "azure/aws/google"));
    }
}