import { notFound } from "next/navigation";
import { Modal } from "@/app/components/Modal";
import { ModalLineItemForm } from "@/app/components/ModalLineItemForm";
import { addLineItemModal } from "@/app/bills-of-quantities/actions";
import {
  getBidBoq,
  getUnitsOfMeasure,
  type BoqStatus,
} from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// A BoQ accepts structural edits only while Draft or Submitted — mirrors the full-page route.
function isEditable(status: BoqStatus): boolean {
  return status === "Draft" || status === "Submitted";
}

// Intercepted overlay for adding a section-level line item. The standalone full-page form at
// the matching route (.../line-items/new) still renders on direct visit or refresh.
export default async function NewLineItemModal({
  params,
}: {
  params: Promise<{ id: string; sectionId: string }>;
}) {
  const { id, sectionId } = await params;
  const boq = await getBidBoq(id);

  if (!boq || !isEditable(boq.status)) {
    notFound();
  }

  const section = boq.sections.find((s) => s.id === sectionId);

  if (!section) {
    notFound();
  }

  // Only active units may be referenced by a new line.
  const allUnits = await getUnitsOfMeasure(true);
  const activeUnits = allUnits.filter((u) => u.isActive);

  return (
    <Modal
      title={t("lineItems.addTitle")}
      subtitle={`${section.sequence}. ${section.name} · ${t("lineItems.addSubtitle")}`}
    >
      {activeUnits.length === 0 ? (
        <p className={styles.muted}>{t("lineItems.noActiveUnits")}</p>
      ) : (
        <ModalLineItemForm
          action={addLineItemModal}
          boqId={boq.id}
          sectionId={section.id}
          currency={boq.pricingCurrency}
          units={activeUnits}
          defaultSequence={section.lineItems.length + 1}
        />
      )}
    </Modal>
  );
}
