namespace HomeProjectManagement.McpServer.Authentication;

/// <summary>
/// Configuration for validating the Cloudflare Access application token at the origin, bound from
/// the <c>CloudflareAccess</c> configuration section (supplied via user-secrets / environment, never
/// committed).
/// </summary>
/// <remarks>
/// Cloudflare Access (with Managed OAuth) is the OAuth 2.0 authorization server and identity-aware
/// proxy in front of this host: it federates login to Google, enforces the stakeholder email
/// allow-list at the edge, and forwards a signed assertion (the <c>Cf-Access-Jwt-Assertion</c>
/// header / <c>CF_Authorization</c> cookie) to the origin. This host's job is reduced to
/// <em>validating that assertion</em> as defense-in-depth and mapping it onto a <c>UserId</c>.
/// <para>
/// When <see cref="Enabled"/> is false (the default for local development) the server runs
/// unauthenticated and network-restricted. Setting it true validates the Access token against
/// Cloudflare's signing keys and re-checks the stakeholder allow-list. This is the seam that "flips
/// on" once the container sits behind the Cloudflare Tunnel.
/// </para>
/// </remarks>
public sealed class CloudflareAccessOptions
{
    public const string SectionName = "CloudflareAccess";

    /// <summary>Whether token validation + the stakeholder allow-list are enforced.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The Cloudflare Access team domain — the token issuer (<c>iss</c>) and the base for the signing
    /// keys at <c>{TeamDomain}/cdn-cgi/access/certs</c>. e.g. <c>https://myteam.cloudflareaccess.com</c>.
    /// </summary>
    public string? TeamDomain { get; set; }

    /// <summary>
    /// The Access application's Audience (AUD) tag — the value the assertion carries in its <c>aud</c>
    /// claim. Cloudflare assigns a unique AUD per application; audience validation enforces it.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// The stakeholder allow-list (verified emails). The primary gate is the Cloudflare Access policy
    /// at the edge; this list is an optional defense-in-depth re-check at the origin. Empty means
    /// "trust the edge policy alone".
    /// </summary>
    public IList<string> AllowedEmails { get; set; } = [];
}
