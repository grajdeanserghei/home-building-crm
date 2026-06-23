import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { LineItemForm } from "@/app/components/LineItemForm";
import { reviseLineItem } from "@/app/bills-of-quantities/actions";
import {
  getBillOfQuantities,
  getUnitsOfMeasure,
  type BoqStatus,
} from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// A BoQ accepts structural edits only while Draft or Submitted — mirrors the aggregate
// and the detail page's `isEditable`.
function isEditable(status: BoqStatus): boolean {
  return status === "Draft" || status === "Submitted";
}

export default async function EditLineItemPage({
  params,
}: {
  params: Promise<{ id: string; sectionId: string; lineItemId: string }>;
}) {
  const { id, sectionId, lineItemId } = await params;
  const boq = await getBillOfQuantities(id);

  if (!boq) {
    notFound();
  }

  // A locked BoQ has no editable lines — bounce back to the read-only detail.
  if (!isEditable(boq.status)) {
    redirect(`/bills-of-quantities/${id}`);
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
    <main className={styles.main}>
      <Link href={`/bills-of-quantities/${boq.id}`} className={styles.backLink}>
        {t("boq.backToBoq")}
      </Link>
      <h1>{t("lineItems.editTitle")}</h1>
      <p className={styles.subtitle}>
        {section.sequence}. {section.name} · {t("lineItems.editSubtitle")}
      </p>

      <section className={styles.card}>
        <LineItemForm
          action={reviseLineItem}
          boqId={boq.id}
          sectionId={section.id}
          currency={boq.pricingCurrency}
          units={units}
          lineItem={lineItem}
          submitLabel={t("common.saveChanges")}
        />
      </section>
    </main>
  );
}
