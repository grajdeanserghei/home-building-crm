import { notFound } from "next/navigation";
import { Modal } from "@/app/components/Modal";
import { ModalLineItemForm } from "@/app/components/ModalLineItemForm";
import { reviseLineItemModal } from "@/app/bills-of-quantities/actions";
import {
  getBillOfQuantities,
  getUnitsOfMeasure,
  type BoqStatus,
} from "@/app/lib/api";
import { t } from "@/app/lib/i18n";

// A BoQ accepts structural edits only while Draft or Submitted — mirrors the full-page route.
function isEditable(status: BoqStatus): boolean {
  return status === "Draft" || status === "Submitted";
}

// Intercepted overlay for editing a section-level line item. The standalone full-page form at
// the matching route (.../line-items/[lineItemId]/edit) still renders on direct visit or refresh.
export default async function EditLineItemModal({
  params,
}: {
  params: Promise<{ id: string; sectionId: string; lineItemId: string }>;
}) {
  const { id, sectionId, lineItemId } = await params;
  const boq = await getBillOfQuantities(id);

  if (!boq || !isEditable(boq.status)) {
    notFound();
  }

  const section = boq.sections.find((s) => s.id === sectionId);
  const lineItem = section?.lineItems.find((li) => li.id === lineItemId);

  if (!section || !lineItem) {
    notFound();
  }

  // Only active units may be referenced; but the line's current unit might since have been
  // retired, so keep it in the list so the select can show (and re-submit) it.
  const allUnits = await getUnitsOfMeasure(true);
  const units = allUnits.filter(
    (u) => u.isActive || u.id === lineItem.unitOfMeasureId,
  );

  return (
    <Modal
      title={t("lineItems.editTitle")}
      subtitle={`${section.sequence}. ${section.name} · ${t("lineItems.editSubtitle")}`}
    >
      <ModalLineItemForm
        action={reviseLineItemModal}
        boqId={boq.id}
        sectionId={section.id}
        currency={boq.pricingCurrency}
        units={units}
        lineItem={lineItem}
        submitLabel={t("common.saveChanges")}
      />
    </Modal>
  );
}
