using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OdectyMVC.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace OdectyMVC;

public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IOptions<BasicAuthentication> basicAuthenticationOptions;

    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<BasicAuthentication> basicAuthenticationOptions)
        : base(options, logger, encoder)
    {
        this.basicAuthenticationOptions = basicAuthenticationOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request!.Headers.ContainsKey("Authorization"))
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));

        if (!AuthenticationHeaderValue.TryParse(Request.Headers["Authorization"]!, out var authHeader)
            || !"Basic".Equals(authHeader.Scheme, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(authHeader.Parameter))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }

        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(authHeader.Parameter);
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }

        var raw = Encoding.UTF8.GetString(decoded);
        var separator = raw.IndexOf(':');
        if (separator < 0)
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));

        var username = raw[..separator];
        var password = raw[(separator + 1)..];

        var client = basicAuthenticationOptions.Value.Clients
            .FirstOrDefault(c => c.Username == username);

        var expected = Encoding.UTF8.GetBytes(client?.Password ?? "\0");
        var actual = Encoding.UTF8.GetBytes(password);
        if (client == null || !CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            Logger.LogWarning("Basic auth failed for user {User} from {RemoteIp}",
                username, Context.Connection.RemoteIpAddress);
            return Task.FromResult(AuthenticateResult.Fail("Invalid credentials"));
        }

        var claims = new List<Claim> { new(ClaimTypes.Name, client.Username) };
        claims.AddRange(client.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
