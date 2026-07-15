namespace Microsoft.Extensions.Hosting;

// The running service's version, for tracking (an OpenTelemetry `service.version` resource
// attribute on every trace/metric/log) and a startup log line. Both the API and the MCP
// server resolve it here via AddServiceDefaults.
//
// The value comes from the APP_VERSION / GIT_SHA env vars stamped into the container image
// at build time by scripts/build-and-push.sh — the same contract the web footer uses
// (see src/web/app/lib/version.ts). Outside a stamped container (local Aspire dev) neither
// is set and the version reads as "dev".
public static class AppVersion
{
    /// <summary>
    /// The resolved version string:
    /// <list type="bullet">
    /// <item>both present → <c>v0.17.0+9de7e29</c> (semver build-metadata form)</item>
    /// <item>only one present → that value</item>
    /// <item>neither (local dev) → <c>dev</c></item>
    /// </list>
    /// </summary>
    public static string Current { get; } = Resolve();

    private static string Resolve()
    {
        var version = Environment.GetEnvironmentVariable("APP_VERSION")?.Trim();
        var sha = Environment.GetEnvironmentVariable("GIT_SHA")?.Trim();

        if (!string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(sha))
        {
            return $"{version}+{sha}";
        }

        if (!string.IsNullOrEmpty(version))
        {
            return version;
        }

        return string.IsNullOrEmpty(sha) ? "dev" : sha;
    }
}
