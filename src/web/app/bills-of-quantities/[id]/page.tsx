import { notFound, redirect } from "next/navigation";
import { getBillOfQuantities } from "@/app/lib/api";

// The BoQ detail was merged into its owning bid's page (/bids/[id]). This route is kept only as a
// stable permalink for a BoQ id (e.g. a link from an awarded contract): it resolves the bill and
// redirects to the bid, preserving the arrange view flag. Display currency is now a global cookie
// (the header toggle), not a URL param, so it needs no forwarding here.
export default async function BillOfQuantitiesPermalink({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ arrange?: string }>;
}) {
  const { id } = await params;
  const { arrange } = await searchParams;
  const boq = await getBillOfQuantities(id);

  if (!boq) {
    notFound();
  }

  const qs = arrange ? `?arrange=${arrange}` : "";
  redirect(`/bids/${boq.bidId}${qs}`);
}
