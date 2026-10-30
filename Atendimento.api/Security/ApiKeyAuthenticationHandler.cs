using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Atendimento.API.Security;

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string HeaderName = "X-API-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var providedApiKey) || string.IsNullOrWhiteSpace(providedApiKey))
            return Task.FromResult(AuthenticateResult.Fail("API key ausente."));

        var configuredApiKey = configuration["Security:ApiKey"];
        if (string.IsNullOrWhiteSpace(configuredApiKey))
            return Task.FromResult(AuthenticateResult.Fail("Configuração Security:ApiKey não encontrada."));

        if (!ApiKeyValidator.IsValid(providedApiKey!, configuredApiKey))
            return Task.FromResult(AuthenticateResult.Fail("API key inválida."));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "api-client"),
            new Claim(ClaimTypes.Name, "api-client")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
