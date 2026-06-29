using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HomeProjectManagement.Application.Abstractions;
using HomeProjectManagement.Domain.Common.ValueObjects;

namespace HomeProjectManagement.McpServer.Identity;

/// <summary>
/// Real <see cref="ICurrentUser"/> adapter for the authenticated MCP host: it maps the Cloudflare
/// Access assertion's subject/email onto the stakeholder's <see cref="UserId"/> so every write is
/// attributed to whoever's token drove it. Because <see cref="ICurrentUser"/> is the existing audit
/// seam, this is a pure Infrastructure-style adapter swap — Application and Domain are untouched.
/// </summary>
/// <remarks>
/// Cloudflare Access carries a stable per-user id in <c>sub</c> (a UUID) plus the verified
/// <c>email</c>. We use <c>sub</c> directly when it is GUID-shaped, otherwise derive a deterministic
/// GUID from the email so the same stakeholder always maps to the same id. Outside an authenticated
/// request it falls back to <see cref="UserId.System"/>.
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

            // Cloudflare Access 'sub' is the user's stable Access id (a UUID); use it directly.
            var sub = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(sub, out var subGuid))
            {
                return new UserId(subGuid);
            }

            // Otherwise derive a stable id from the verified email.
            var key = principal.FindFirstValue("email")
                      ?? principal.FindFirstValue(ClaimTypes.Email)
                      ?? sub;
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
