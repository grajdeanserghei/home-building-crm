import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { LineItemForm } from "@/app/components/LineItemForm";
import { reviseSubsectionLineItem } from "@/app/bills-of-quantities/actions";
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

export default async function EditSubsectionLineItemPage({
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
  const boq = await getBillOfQuantities(id);

  if (!boq) {
    notFound();
  }

  // A locked BoQ has no editable lines — bounce back to the read-only detail.
  if (!isEditable(boq.status)) {
    redirect(`/bills-of-quantities/${id}`);
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
    <main className={styles.main}>
      <Link href={`/bills-of-quantities/${boq.id}`} className={styles.backLink}>
        {t("boq.backToBoq")}
      </Link>
      <h1>{t("lineItems.editTitle")}</h1>
      <p className={styles.subtitle}>
        {section.sequence}.{subsection.sequence} {section.name} ·{" "}
        {subsection.name} · {t("lineItems.editSubtitle")}
      </p>

      <section className={styles.card}>
        <LineItemForm
          action={reviseSubsectionLineItem}
          boqId={boq.id}
          sectionId={section.id}
          subsectionId={subsection.id}
          currency={boq.pricingCurrency}
          units={units}
          lineItem={lineItem}
          submitLabel={t("common.saveChanges")}
        />
      </section>
    </main>
  );
}
