using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace DocumentProcessorApi.Authentication
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private const string Username = "vs";
        private const string Password = "rekrutacja";

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        { }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Check if the Authorization header is present
            if (!Request.Headers.TryGetValue("Authorization", out var value))
                return AuthenticateResult.Fail("Missing Authorization Header.");

            try
            {
                var authHeader = AuthenticationHeaderValue.Parse(value);
                if (!authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
                    return AuthenticateResult.Fail("Invalid Authorization Scheme.");

                var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
                if (credentials.Length != 2)
                    return AuthenticateResult.Fail("Invalid Authorization Header Format.");

                var username = credentials[0];
                var password = credentials[1];

                // Validate username and password
                if (username != Username || password != Password)
                    return AuthenticateResult.Fail("Invalid Username or Password.");

                // Create claims
                var claims = new[] {
                    new Claim(ClaimTypes.NameIdentifier, username),
                    new Claim(ClaimTypes.Name, username)
                };
                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return AuthenticateResult.Success(ticket);
            }
            catch
            {
                return AuthenticateResult.Fail("Invalid Authorization Header.");
            }
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;
            Response.Headers.Append("WWW-Authenticate", "Basic realm=\"DocumentProcessorApi\"");
            return Task.CompletedTask;
        }
    }
}
