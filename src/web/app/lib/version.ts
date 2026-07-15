// The running application's version, for display in the footer.
//
// The value is stamped into the web container at build time by
// scripts/build-and-push.sh, which passes the release semver (`--version`, e.g.
// "v0.17.0") and the git short-SHA (e.g. "9de7e29") as Docker build-args that the
// Dockerfile exposes as the APP_VERSION / GIT_SHA env vars. These are read
// server-side at request time (like API_BASE_URL) — no NEXT_PUBLIC_ prefix needed.
//
// Outside a stamped container (local Aspire dev), neither var is set and we fall
// back to "dev".

/**
 * The version label shown in the footer:
 *   - both present → "v0.17.0 · 9de7e29"
 *   - only one present → that value
 *   - neither (local dev) → "dev"
 */
export function appVersionLabel(): string {
  const version = process.env.APP_VERSION?.trim();
  const sha = process.env.GIT_SHA?.trim();
  if (version && sha) {
    return `${version} · ${sha}`;
  }
  return version || sha || "dev";
}
