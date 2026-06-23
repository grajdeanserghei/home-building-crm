import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { SubsectionForm } from "@/app/components/SubsectionForm";
import { updateSubsection } from "@/app/bills-of-quantities/actions";
import { getBillOfQuantities, type BoqStatus } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// A BoQ accepts structural edits only while Draft or Submitted — mirrors the aggregate
// and the detail page's `isEditable`.
function isEditable(status: BoqStatus): boolean {
  return status === "Draft" || status === "Submitted";
}

export default async function EditSubsectionPage({
  params,
}: {
  params: Promise<{ id: string; sectionId: string; subsectionId: string }>;
}) {
  const { id, sectionId, subsectionId } = await params;
  const boq = await getBillOfQuantities(id);

  if (!boq) {
    notFound();
  }

  // A locked BoQ has no editable structure — bounce back to the read-only detail.
  if (!isEditable(boq.status)) {
    redirect(`/bills-of-quantities/${id}`);
  }

  const section = boq.sections.find((s) => s.id === sectionId);
  const subsection = section?.subsections.find((s) => s.id === subsectionId);

  if (!section || !subsection) {
    notFound();
  }

  return (
    <main className={styles.main}>
      <Link href={`/bills-of-quantities/${boq.id}`} className={styles.backLink}>
        {t("boq.backToBoq")}
      </Link>
      <h1>{t("subsections.editTitle")}</h1>
      <p className={styles.subtitle}>
        {section.sequence}.{subsection.sequence} {subsection.name} ·{" "}
        {t("subsections.editSubtitle")}
      </p>

      <section className={styles.card}>
        <SubsectionForm
          action={updateSubsection}
          boqId={boq.id}
          sectionId={section.id}
          subsection={subsection}
          submitLabel={t("common.saveChanges")}
        />
      </section>
    </main>
  );
}
