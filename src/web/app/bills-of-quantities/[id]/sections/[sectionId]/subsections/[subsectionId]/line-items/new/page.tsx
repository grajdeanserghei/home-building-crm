import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { LineItemForm } from "@/app/components/LineItemForm";
import { addSubsectionLineItem } from "@/app/bills-of-quantities/actions";
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

export default async function NewSubsectionLineItemPage({
  params,
}: {
  params: Promise<{ id: string; sectionId: string; subsectionId: string }>;
}) {
  const { id, sectionId, subsectionId } = await params;
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

  if (!section || !subsection) {
    notFound();
  }

  // Only active units may be referenced by a new line.
  const allUnits = await getUnitsOfMeasure(true);
  const activeUnits = allUnits.filter((u) => u.isActive);

  return (
    <main className={styles.main}>
      <Link href={`/bills-of-quantities/${boq.id}`} className={styles.backLink}>
        {t("boq.backToBoq")}
      </Link>
      <h1>{t("lineItems.addTitle")}</h1>
      <p className={styles.subtitle}>
        {section.sequence}.{subsection.sequence} {subsection.name} ·{" "}
        {t("lineItems.addSubtitle")}
      </p>

      <section className={styles.card}>
        {activeUnits.length === 0 ? (
          <p className={styles.muted}>{t("lineItems.noActiveUnits")}</p>
        ) : (
          <LineItemForm
            action={addSubsectionLineItem}
            boqId={boq.id}
            sectionId={section.id}
            subsectionId={subsection.id}
            currency={boq.pricingCurrency}
            units={activeUnits}
            defaultSequence={subsection.lineItems.length + 1}
          />
        )}
      </section>
    </main>
  );
}
