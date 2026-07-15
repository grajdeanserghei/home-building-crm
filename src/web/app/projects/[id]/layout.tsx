// Layout for a single project. It exists to host the `@modal` parallel slot used by the
// intercepting valuation-item routes under `valuation/`: `children` is the page (a project
// detail/section page, or a full-page item form on direct visit/refresh) and `modal` is the
// overlay shown when an add/revise-item link is followed client-side. On any route the slot
// doesn't intercept, `modal` resolves to its `default.tsx` (null), so this is otherwise a
// transparent passthrough — every existing /projects/[id]/… route renders unchanged.
export default function ProjectLayout({
  children,
  modal,
}: {
  children: React.ReactNode;
  modal: React.ReactNode;
}) {
  return (
    <>
      {children}
      {modal}
    </>
  );
}
