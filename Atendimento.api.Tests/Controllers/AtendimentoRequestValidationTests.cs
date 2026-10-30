using System.ComponentModel.DataAnnotations;
using Atendimento.API.Controllers;

namespace Atendimento.API.Tests.Controllers;

public class AtendimentoRequestValidationTests
{
    [Fact]
    public void AtendimentoRequest_WhenFieldsAreValid_IsValid()
    {
        var request = new AtendimentoRequest("César", "Gostaria de saber as próximas turmas de .NET");

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(request);

        var valid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.True(valid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void AtendimentoRequest_WhenFieldsAreInvalid_IsInvalid()
    {
        var request = new AtendimentoRequest("", "oi");

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(request);

        var valid = Validator.TryValidateObject(request, context, validationResults, true);

        Assert.False(valid);
        Assert.NotEmpty(validationResults);
    }
}
