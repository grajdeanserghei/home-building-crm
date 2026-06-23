using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.McpServer.Identity;

/// <summary>
/// Real <see cref="ICurrentUser"/> adapter for the authenticated MCP host: it maps the bearer
/// token's subject/email onto the stakeholder's <see cref="UserId"/> so every write is attributed
/// to whoever's token drove it. Because <see cref="ICurrentUser"/> is the existing audit seam, this
/// is a pure Infrastructure-style adapter swap — Application and Domain are untouched.
/// </summary>
/// <remarks>
/// Entra External ID issues an object id (<c>oid</c>) per user — a stable GUID we use directly as
/// the <see cref="UserId"/>. When no GUID-shaped claim is present we derive a deterministic GUID
/// from <c>sub</c>/email so the same stakeholder always maps to the same id. Outside an
/// authenticated request it falls back to <see cref="UserId.System"/>.
/// </remarks>
public sealed class PrincipalCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public UserId UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity is not { IsAuthenticated: true })
            {
                return UserId.System;
            }

            // Entra object id is a GUID identifying the user; prefer it.
            var oid = principal.FindFirstValue("oid")
                      ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");
            if (Guid.TryParse(oid, out var oidGuid))
            {
                return new UserId(oidGuid);
            }

            var sub = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(sub, out var subGuid))
            {
                return new UserId(subGuid);
            }

            var key = sub
                      ?? principal.FindFirstValue("email")
                      ?? principal.FindFirstValue(ClaimTypes.Email);
            return key is null ? UserId.System : new UserId(DeterministicGuid(key));
        }
    }

    // A stable GUID from an arbitrary identity string (not for security — just a consistent mapping).
    private static Guid DeterministicGuid(string value)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()));
        return new Guid(hash);
    }
}
