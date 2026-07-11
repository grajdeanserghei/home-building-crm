import { notFound, redirect } from "next/navigation";
import { getBillOfQuantities } from "@/app/lib/api";

// The BoQ detail was merged into its owning bid's page (/bids/[id]). This route is kept only as a
// stable permalink for a BoQ id (e.g. a link from an awarded contract): it resolves the bill and
// redirects to the bid, preserving the currency/arrange view flags.
export default async function BillOfQuantitiesPermalink({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ arrange?: string; currency?: string }>;
}) {
  const { id } = await params;
  const { arrange, currency } = await searchParams;
  const boq = await getBillOfQuantities(id);

  if (!boq) {
    notFound();
  }

  const query = new URLSearchParams();
  if (currency) query.set("currency", currency);
  if (arrange) query.set("arrange", arrange);
  const qs = query.toString();
  redirect(`/bids/${boq.bidId}${qs ? `?${qs}` : ""}`);
}
