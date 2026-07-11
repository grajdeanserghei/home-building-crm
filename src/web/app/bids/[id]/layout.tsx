// Layout for a single bid. It exists to host the `@modal` parallel slot used by the
// intercepting line-item routes under `boq/`: `children` is the page (the merged bid + BoQ
// detail, or a full-page form on direct visit/refresh) and `modal` is the overlay shown when a
// line-item add/edit link is followed client-side. On any route the slot doesn't intercept,
// `modal` resolves to its `default.tsx` (null), so this is otherwise a transparent passthrough.
export default function BidLayout({
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
