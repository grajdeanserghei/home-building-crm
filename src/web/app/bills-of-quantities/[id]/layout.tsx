// Layout for a single BoQ. It exists to host the `@modal` parallel slot used by the
// intercepting line-item routes: `children` is the page (the read-first detail, or a
// full-page form on direct visit/refresh) and `modal` is the overlay shown when an
// add/edit link is followed client-side. On any route the slot doesn't intercept, `modal`
// resolves to its `default.tsx` (null), so this is otherwise a transparent passthrough.
export default function BillOfQuantitiesLayout({
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
