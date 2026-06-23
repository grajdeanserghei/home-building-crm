namespace HomeProjectManagement.McpServer.Authentication;

/// <summary>
/// Configuration for the MCP server's OAuth 2.1 resource-server role, bound from the
/// <c>McpAuth</c> configuration section (supplied via user-secrets / environment, never committed).
/// </summary>
/// <remarks>
/// When <see cref="Enabled"/> is false (the default for local development) the server runs
/// unauthenticated and network-restricted — exactly the "initially network-restricted" posture of
/// the spec's implementation order. Setting <see cref="Enabled"/> to true turns the host into a
/// resource server that validates Entra External ID bearer tokens and restricts access to the four
/// stakeholders. This is the seam that "flips on" before <c>.WithExternalHttpEndpoints()</c>.
/// </remarks>
public sealed class McpAuthOptions
{
    public const string SectionName = "McpAuth";

    /// <summary>Whether token validation + the stakeholder allow-list are enforced.</summary>
    public bool Enabled { get; set; }

    /// <summary>The OAuth authorization server (Entra External ID tenant authority/issuer).</summary>
    public string? Authority { get; set; }

    /// <summary>
    /// This server's resource identifier — the value bound tokens carry in their <c>aud</c> claim
    /// (RFC 8707 resource indicator). JWT-bearer audience validation enforces it.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// The canonical public URL of this MCP resource, advertised in the protected-resource metadata
    /// (RFC 9728). Defaults to <see cref="Audience"/> when not set.
    /// </summary>
    public string? ResourceUri { get; set; }

    /// <summary>The single scope sufficient for the four trusted stakeholders.</summary>
    public string RequiredScope { get; set; } = "project:write";

    /// <summary>
    /// The stakeholder allow-list (verified emails). Empty means "rely solely on the tenant's
    /// limited membership"; when populated, the authorization policy additionally checks the email.
    /// </summary>
    public IList<string> AllowedEmails { get; set; } = [];
}
