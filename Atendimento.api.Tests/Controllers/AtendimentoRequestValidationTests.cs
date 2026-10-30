using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Atendimento.API.Controllers;
using Xunit;

namespace Atendimento.API.Tests.Controllers;

public class AtendimentoRequestValidationTests
{
    [Fact]
    public void AtendimentoRequest_ConstructorParameters_HaveValidationAttributes()
    {
        var constructor = typeof(AtendimentoRequest).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();

        var nome = constructor.GetParameters().Single(x => x.Name == "Nome");
        var mensagem = constructor.GetParameters().Single(x => x.Name == "Mensagem");

        Assert.Contains(nome.GetCustomAttributes<RequiredAttribute>(), _ => true);
        Assert.Contains(nome.GetCustomAttributes<MinLengthAttribute>(), x => x.Length == 2);
        Assert.Contains(nome.GetCustomAttributes<MaxLengthAttribute>(), x => x.Length == 120);

        Assert.Contains(mensagem.GetCustomAttributes<RequiredAttribute>(), _ => true);
        Assert.Contains(mensagem.GetCustomAttributes<MinLengthAttribute>(), x => x.Length == 5);
        Assert.Contains(mensagem.GetCustomAttributes<MaxLengthAttribute>(), x => x.Length == 2000);
    }

    [Fact]
    public void AtendimentoRequest_Properties_DoNotHaveValidationAttributes()
    {
        var propertyAttributes = typeof(AtendimentoRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(x => x.GetCustomAttributes<ValidationAttribute>(inherit: true))
            .ToList();

        Assert.Empty(propertyAttributes);
    }
}
