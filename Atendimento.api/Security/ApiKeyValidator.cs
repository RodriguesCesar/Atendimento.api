namespace Atendimento.API.Security;

public static class ApiKeyValidator
{
    public static bool IsValid(string providedApiKey, string configuredApiKey)
    {
        if (string.IsNullOrWhiteSpace(providedApiKey) || string.IsNullOrWhiteSpace(configuredApiKey))
            return false;

        return string.Equals(providedApiKey.Trim(), configuredApiKey.Trim(), StringComparison.Ordinal);
    }
}
