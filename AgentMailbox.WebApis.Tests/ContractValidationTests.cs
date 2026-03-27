using AgentMailbox.Core.Contracts;
using System.ComponentModel.DataAnnotations;

namespace AgentMailbox.WebApis.Tests;

public sealed class ContractValidationTests
{
    [Fact]
    public void CreateMailboxRequest_ShouldFailValidation_WhenAddressIsInvalid()
    {
        var request = new CreateMailboxRequest
        {
            Address = "not-an-email",
            DisplayName = "Support Agent"
        };

        var validationResults = Validate(request);

        Assert.NotEmpty(validationResults);
    }

    [Fact]
    public void SendMailRequest_ShouldPassValidation_WhenRequiredFieldsAreProvided()
    {
        var request = new SendMailRequest
        {
            MailboxAddress = "support-agent@local.ai",
            ToAddress = "user@example.com",
            Subject = "Welcome",
            BodyText = "Hello there"
        };

        var validationResults = Validate(request);

        Assert.Empty(validationResults);
    }

    private static IReadOnlyList<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }
}
