import { notFound } from "next/navigation";
import { Modal } from "@/app/components/Modal";
import { getValuationCatalog } from "@/app/lib/api";
import { ValuationCatalogItemForm } from "@/app/components/ValuationCatalogItemForm";
import { reviseValuationCatalogItem } from "@/app/projects/[id]/valuation/actions";
import { t } from "@/app/lib/i18n";

// Intercepted overlay for revising a valuation catalog item. The standalone full-page form at
// the matching route still renders on direct visit or refresh.
export default async function EditValuationItemModal({
  params,
}: {
  params: Promise<{ id: string; itemId: string }>;
}) {
  const { id, itemId } = await params;
  const catalog = await getValuationCatalog(id);
  const item = catalog?.items.find((i) => i.id === itemId);

  if (!catalog || !item) {
    notFound();
  }

  return (
    <Modal title={t("valuation.item.editTitle")} subtitle={item.name}>
      <ValuationCatalogItemForm
        action={reviseValuationCatalogItem}
        projectId={id}
        catalogId={catalog.id}
        currency={catalog.currency}
        item={item}
        submitLabel={t("valuation.item.save")}
      />
    </Modal>
  );
}
