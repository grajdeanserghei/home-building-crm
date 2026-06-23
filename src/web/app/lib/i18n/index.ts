// Thin Romanian-only i18n helper. No i18n library is needed for a single locale — this is
// a lookup against the `ro` catalog plus {placeholder} interpolation. If English returns
// later, this becomes a `messages[locale]` lookup — additive, not a rewrite.
// See docs/specifications/romanian-localization.md.
import { ro } from "./ro";

export { ro } from "./ro";
export type { RoCatalog } from "./ro";

export type MessageKey = keyof typeof ro;

/**
 * Translate a catalog key to Romanian, interpolating `{placeholder}` tokens from `params`.
 * Unknown keys return the key itself (visible-but-safe), so a missing string never crashes
 * a render. Known keys are type-checked; arbitrary strings (e.g. a dynamic error code) are
 * also accepted for the graceful-degradation paths.
 */
export function t(
  key: MessageKey | (string & {}),
  params?: Record<string, string | number>,
): string {
  const template = (ro as Record<string, string>)[key] ?? key;
  if (!params) {
    return template;
  }
  return template.replace(/\{(\w+)\}/g, (match, name) =>
    name in params ? String(params[name]) : match,
  );
}
