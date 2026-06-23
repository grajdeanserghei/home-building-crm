using System.Security.Claims;
using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.McpServer.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;

namespace HomeProjectManagement.McpServer.Authentication;

/// <summary>
/// Wires the MCP server's OAuth 2.1 resource-server role: JWT bearer validation against Entra
/// External ID, protected-resource metadata (RFC 9728) so clients can discover the authorization
/// server, a stakeholder allow-list authorization policy, and the real
/// <see cref="ICurrentUser"/> adapter. All of it is gated by <see cref="McpAuthOptions.Enabled"/>
/// so the host runs network-restricted and unauthenticated in local development.
/// </summary>
public static class McpAuthExtensions
{
    /// <summary>Authorization policy name applied to the MCP endpoint when auth is enabled.</summary>
    public const string StakeholderPolicy = "Stakeholders";

    /// <summary>
    /// Configures resource-server authentication if <c>McpAuth:Enabled</c> is true. Returns true when
    /// authentication is enabled (so the caller knows to require authorization on the MCP endpoint).
    /// </summary>
    public static bool AddMcpResourceServerAuthentication(this WebApplicationBuilder builder)
    {
        var options = new McpAuthOptions();
        builder.Configuration.GetSection(McpAuthOptions.SectionName).Bind(options);

        if (!options.Enabled)
        {
            // Network-restricted dev posture: keep the StubCurrentUser registered by AddInfrastructure.
            return false;
        }

        if (string.IsNullOrWhiteSpace(options.Authority) || string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException(
                "McpAuth:Enabled is true but McpAuth:Authority and McpAuth:Audience are not configured.");
        }

        var resourceUri = options.ResourceUri ?? options.Audience;

        builder.Services
            .AddAuthentication(auth =>
            {
                // The MCP scheme owns the 401 challenge (emits WWW-Authenticate → protected-resource
                // metadata); JWT bearer validates the presented token.
                auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                auth.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(jwt =>
            {
                jwt.Authority = options.Authority;
                jwt.Audience = options.Audience;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    // RFC 8707: the resource-bound token must carry this server's id as its audience.
                    ValidAudience = options.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
            })
            .AddMcp(mcp =>
            {
                // RFC 9728 protected-resource metadata, hosted at /.well-known/oauth-protected-resource.
                mcp.ResourceMetadata = new ProtectedResourceMetadata
                {
                    Resource = resourceUri,
                    AuthorizationServers = { options.Authority },
                    ScopesSupported = [options.RequiredScope]
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

                // The single trusted scope (delegated scopes arrive in the space-delimited "scp" claim).
                policy.RequireAssertion(context =>
                    HasScope(context.User, options.RequiredScope));

                // Restrict to the four stakeholders by verified email, on top of the tenant's limited
                // membership. An empty allow-list relies on tenant membership alone.
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

    private static bool HasScope(ClaimsPrincipal user, string required)
    {
        if (string.IsNullOrWhiteSpace(required))
        {
            return true;
        }

        var scp = user.FindFirst("scp")?.Value ?? user.FindFirst("scope")?.Value;
        return scp is not null
               && scp.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Contains(required, StringComparer.OrdinalIgnoreCase);
    }

    private static string? EmailOf(ClaimsPrincipal user) =>
        user.FindFirst("email")?.Value
        ?? user.FindFirst(ClaimTypes.Email)?.Value
        ?? user.FindFirst("preferred_username")?.Value;
}
