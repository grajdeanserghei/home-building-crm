import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { SubsectionForm } from "@/app/components/SubsectionForm";
import { addSubsection } from "@/app/bills-of-quantities/actions";
import { getBidBoq, type BoqStatus } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// A BoQ accepts structural edits only while Draft or Submitted — mirrors the aggregate
// and the detail page's `isEditable`.
function isEditable(status: BoqStatus): boolean {
  return status === "Draft" || status === "Submitted";
}

export default async function NewSubsectionPage({
  params,
}: {
  params: Promise<{ id: string; sectionId: string }>;
}) {
  const { id, sectionId } = await params;
  const boq = await getBidBoq(id);

  if (!boq) {
    notFound();
  }

  // A locked BoQ has no editable structure — bounce back to the read-only detail.
  if (!isEditable(boq.status)) {
    redirect(`/bids/${id}`);
  }

  const section = boq.sections.find((s) => s.id === sectionId);

  if (!section) {
    notFound();
  }

  return (
    <main className={styles.main}>
      <Link href={`/bids/${id}`} className={styles.backLink}>
        {t("boq.backToBoq")}
      </Link>
      <h1>{t("subsections.addTitle")}</h1>
      <p className={styles.subtitle}>
        {section.sequence}. {section.name} · {t("subsections.addSubtitle")}
      </p>

      <section className={styles.card}>
        <SubsectionForm
          action={addSubsection}
          boqId={boq.id}
          sectionId={section.id}
          defaultSequence={section.subsections.length + 1}
        />
      </section>
    </main>
  );
}
