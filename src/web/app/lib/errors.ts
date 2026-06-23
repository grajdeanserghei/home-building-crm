// Turns a backend error response into a Romanian, user-facing message.
//
// The domain core stays English/pure: a violated rule reaches the wire as an RFC 7807
// ProblemDetails carrying a stable `code` (+ optional `params`) plus an English `detail`.
// Here we translate: known codes render their Romanian template (re-interpolating params),
// and the long tail of untranslated codes degrades gracefully to the English `detail`, then
// to a generic message. See docs/specifications/romanian-localization.md → Error messages.
import { t, type MessageKey } from "./i18n";

// The ProblemDetails shape the backend returns, plus our domain extensions (code/params).
interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
  params?: Record<string, string | number>;
}

/**
 * Read an error `Response` and produce a Romanian message:
 *   1. a known domain `code` → its Romanian template (params re-interpolated);
 *   2. else the English `detail` from the ProblemDetails (partial coverage is safe);
 *   3. else a generic message built from the HTTP status via `fallbackKey`.
 */
export async function describeApiError(
  res: Response,
  fallbackKey: MessageKey = "common.apiError",
): Promise<string> {
  let problem: ProblemDetails | null = null;
  try {
    const body = await res.clone().json();
    if (body && typeof body === "object") {
      problem = body as ProblemDetails;
    }
  } catch {
    // Non-JSON body (e.g. an HTML 500 page) — fall through to the status-based fallback.
  }

  if (problem?.code) {
    const key = `errors.${problem.code}`;
    const localized = t(key, problem.params);
    if (localized !== key) {
      return localized; // a Romanian template existed for this code
    }
  }

  if (problem?.detail) {
    return problem.detail; // English developer-facing fallback for the untranslated tail
  }

  return t(fallbackKey, { error: `${res.status} ${res.statusText}`.trim() });
}
