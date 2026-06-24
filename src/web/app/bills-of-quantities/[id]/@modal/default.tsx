// Fallback for the `@modal` slot when the current route isn't one of the intercepted
// line-item forms (i.e. almost always). Rendering null keeps the overlay closed; it also
// covers hard navigations, where interception doesn't run and the real full-page form renders
// in `children` instead.
export default function ModalDefault() {
  return null;
}
