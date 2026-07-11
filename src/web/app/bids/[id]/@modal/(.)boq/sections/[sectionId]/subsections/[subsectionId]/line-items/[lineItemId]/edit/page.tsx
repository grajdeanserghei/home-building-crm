import { notFound } from "next/navigation";
import { Modal } from "@/app/components/Modal";
import { ModalLineItemForm } from "@/app/components/ModalLineItemForm";
import { reviseSubsectionLineItemModal } from "@/app/bills-of-quantities/actions";
import {
  getBidBoq,
  getUnitsOfMeasure,
  type BoqStatus,
} from "@/app/lib/api";
import { t } from "@/app/lib/i18n";

// A BoQ accepts structural edits only while Draft or Submitted — mirrors the full-page route.
function isEditable(status: BoqStatus): boolean {
  return status === "Draft" || status === "Submitted";
}

// Intercepted overlay for editing a subsection-level line item. The standalone full-page form at
// the matching route still renders on direct visit or refresh.
export default async function EditSubsectionLineItemModal({
  params,
}: {
  params: Promise<{
    id: string;
    sectionId: string;
    subsectionId: string;
    lineItemId: string;
  }>;
}) {
  const { id, sectionId, subsectionId, lineItemId } = await params;
  const boq = await getBidBoq(id);

  if (!boq || !isEditable(boq.status)) {
    notFound();
  }

  const section = boq.sections.find((s) => s.id === sectionId);
  const subsection = section?.subsections.find((s) => s.id === subsectionId);
  const lineItem = subsection?.lineItems.find((li) => li.id === lineItemId);

  if (!section || !subsection || !lineItem) {
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
      subtitle={`${section.sequence}.${subsection.sequence} ${section.name} · ${subsection.name} · ${t("lineItems.editSubtitle")}`}
    >
      <ModalLineItemForm
        action={reviseSubsectionLineItemModal}
        boqId={boq.id}
        sectionId={section.id}
        subsectionId={subsection.id}
        currency={boq.pricingCurrency}
        units={units}
        lineItem={lineItem}
        submitLabel={t("common.saveChanges")}
      />
    </Modal>
  );
}
