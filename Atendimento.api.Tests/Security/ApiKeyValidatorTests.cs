using Atendimento.API.Security;
using Xunit;

namespace Atendimento.API.Tests.Security;

public class ApiKeyValidatorTests
{
    [Fact]
    public void IsValid_WhenKeysMatch_ReturnsTrue()
    {
        var valid = ApiKeyValidator.IsValid("abc123", "abc123");

        Assert.True(valid);
    }

    [Theory]
    [InlineData("", "abc")]
    [InlineData("abc", "")]
    [InlineData("abc", "def")]
    public void IsValid_WhenKeysAreInvalid_ReturnsFalse(string provided, string configured)
    {
        var valid = ApiKeyValidator.IsValid(provided, configured);

        Assert.False(valid);
    }
}
