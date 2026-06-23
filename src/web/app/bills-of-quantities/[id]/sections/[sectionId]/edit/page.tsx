import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { SectionForm } from "@/app/components/SectionForm";
import { updateSection } from "@/app/bills-of-quantities/actions";
import { getBillOfQuantities, type BoqStatus } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// A BoQ accepts structural edits only while Draft or Submitted — mirrors the aggregate
// and the detail page's `isEditable`.
function isEditable(status: BoqStatus): boolean {
  return status === "Draft" || status === "Submitted";
}

export default async function EditSectionPage({
  params,
}: {
  params: Promise<{ id: string; sectionId: string }>;
}) {
  const { id, sectionId } = await params;
  const boq = await getBillOfQuantities(id);

  if (!boq) {
    notFound();
  }

  // A locked BoQ has no editable structure — bounce back to the read-only detail.
  if (!isEditable(boq.status)) {
    redirect(`/bills-of-quantities/${id}`);
  }

  const section = boq.sections.find((s) => s.id === sectionId);

  if (!section) {
    notFound();
  }

  return (
    <main className={styles.main}>
      <Link href={`/bills-of-quantities/${boq.id}`} className={styles.backLink}>
        {t("boq.backToBoq")}
      </Link>
      <h1>{t("sections.editTitle")}</h1>
      <p className={styles.subtitle}>
        {section.sequence}. {section.name} · {t("sections.editSubtitle")}
      </p>

      <section className={styles.card}>
        <SectionForm
          action={updateSection}
          boqId={boq.id}
          section={section}
          submitLabel={t("common.saveChanges")}
        />
      </section>
    </main>
  );
}
