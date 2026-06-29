using System.Security.Claims;
using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.McpServer.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace HomeProjectManagement.McpServer.Authentication;

/// <summary>
/// Validates the Cloudflare Access application token at the origin and maps it onto the audit
/// identity. Cloudflare Access (with Managed OAuth) is the OAuth 2.0 authorization server and
/// identity-aware proxy in front of this host — it runs the client's Authorization Code + PKCE flow,
/// federates login to Google, enforces the stakeholder email allow-list at the edge, and forwards a
/// signed <c>Cf-Access-Jwt-Assertion</c> to the origin. This host therefore only needs to
/// <em>validate that assertion</em> (defense-in-depth for any in-cluster path that bypasses the
/// tunnel), re-check the allow-list, and swap the real <see cref="ICurrentUser"/> in. All of it is
/// gated by <see cref="CloudflareAccessOptions.Enabled"/> so the host runs network-restricted and
/// unauthenticated in local development.
/// </summary>
public static class CloudflareAccessAuthExtensions
{
    /// <summary>Authorization policy name applied to the MCP endpoint when auth is enabled.</summary>
    public const string StakeholderPolicy = "Stakeholders";

    /// <summary>
    /// Configures Cloudflare Access token validation if <c>CloudflareAccess:Enabled</c> is true.
    /// Returns true when authentication is enabled (so the caller knows to require authorization on
    /// the MCP endpoint).
    /// </summary>
    public static bool AddCloudflareAccessAuthentication(this WebApplicationBuilder builder)
    {
        var options = new CloudflareAccessOptions();
        builder.Configuration.GetSection(CloudflareAccessOptions.SectionName).Bind(options);

        if (!options.Enabled)
        {
            // Network-restricted dev posture: keep the StubCurrentUser registered by AddInfrastructure.
            return false;
        }

        if (string.IsNullOrWhiteSpace(options.TeamDomain) || string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException(
                "CloudflareAccess:Enabled is true but CloudflareAccess:TeamDomain and "
                + "CloudflareAccess:Audience are not configured.");
        }

        var teamDomain = options.TeamDomain.TrimEnd('/');
        // Cloudflare publishes the signing keys as a raw JWKS (not an OIDC discovery document), so we
        // wrap them into an OpenIdConnectConfiguration ourselves; ConfigurationManager handles caching
        // and key rotation (Cloudflare keeps the current + previous key live).
        var certsUrl = $"{teamDomain}/cdn-cgi/access/certs";

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.MapInboundClaims = false;
                jwt.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    certsUrl,
                    new CloudflareCertsRetriever(),
                    new HttpDocumentRetriever { RequireHttps = true });

                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = teamDomain,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    NameClaimType = "email",
                };

                jwt.Events = new JwtBearerEvents
                {
                    // Cloudflare Access delivers the assertion in its own header / cookie, not the
                    // standard Authorization: Bearer header.
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Headers["Cf-Access-Jwt-Assertion"].FirstOrDefault();
                        if (string.IsNullOrEmpty(token))
                        {
                            token = context.Request.Cookies["CF_Authorization"];
                        }

                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        var allowedEmails = options.AllowedEmails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        builder.Services
            .AddAuthorizationBuilder()
            .AddPolicy(StakeholderPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();

                // Restrict to the four stakeholders by verified email. The Cloudflare Access policy
                // is the primary gate; this re-check is defense-in-depth. An empty allow-list trusts
                // the edge policy alone.
                if (allowedEmails.Count > 0)
                {
                    policy.RequireAssertion(context =>
                    {
                        var email = EmailOf(context.User);
                        return email is not null && allowedEmails.Contains(email);
                    });
                }
            });

        // Swap the audit identity from the fixed stub to the authenticated principal.
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, PrincipalCurrentUser>();

        return true;
    }

    private static string? EmailOf(ClaimsPrincipal user) =>
        user.FindFirst("email")?.Value
        ?? user.FindFirst(ClaimTypes.Email)?.Value;

    /// <summary>
    /// Loads Cloudflare Access's JWKS (<c>/cdn-cgi/access/certs</c>) into the signing keys of an
    /// <see cref="OpenIdConnectConfiguration"/>, so the JWT-bearer handler can validate the
    /// assertion's signature without a full OIDC discovery document.
    /// </summary>
    private sealed class CloudflareCertsRetriever : IConfigurationRetriever<OpenIdConnectConfiguration>
    {
        public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
            string address, IDocumentRetriever retriever, CancellationToken cancel)
        {
            var json = await retriever.GetDocumentAsync(address, cancel).ConfigureAwait(false);
            var config = new OpenIdConnectConfiguration();
            foreach (var key in new JsonWebKeySet(json).GetSigningKeys())
            {
                config.SigningKeys.Add(key);
            }

            return config;
        }
    }
}
