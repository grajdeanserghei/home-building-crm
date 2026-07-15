import { notFound } from "next/navigation";
import { Modal } from "@/app/components/Modal";
import { getValuationCatalog } from "@/app/lib/api";
import { ValuationCatalogItemForm } from "@/app/components/ValuationCatalogItemForm";
import { addValuationCatalogItem } from "@/app/projects/[id]/valuation/actions";
import { t } from "@/app/lib/i18n";

// Intercepted overlay for adding a valuation catalog item. The standalone full-page form at the
// matching route (.../valuation/items/new) still renders on direct visit or refresh.
export default async function NewValuationItemModal({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const catalog = await getValuationCatalog(id);

  if (!catalog) {
    notFound();
  }

  const nextSequence =
    catalog.items.reduce((max, i) => Math.max(max, i.sequence), 0) + 1;

  return (
    <Modal title={t("valuation.item.addTitle")}>
      <ValuationCatalogItemForm
        action={addValuationCatalogItem}
        projectId={id}
        catalogId={catalog.id}
        currency={catalog.currency}
        defaultSequence={nextSequence}
        submitLabel={t("valuation.item.save")}
      />
    </Modal>
  );
}
