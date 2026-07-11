import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { SectionForm } from "@/app/components/SectionForm";
import { addSection } from "@/app/bills-of-quantities/actions";
import { getBidBoq, type BoqStatus } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// A BoQ accepts structural edits only while Draft or Submitted — mirrors the aggregate
// and the detail page's `isEditable`.
function isEditable(status: BoqStatus): boolean {
  return status === "Draft" || status === "Submitted";
}

export default async function NewSectionPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const boq = await getBidBoq(id);

  if (!boq) {
    notFound();
  }

  // A locked BoQ has no editable structure — bounce back to the read-only detail.
  if (!isEditable(boq.status)) {
    redirect(`/bids/${id}`);
  }

  return (
    <main className={styles.main}>
      <Link href={`/bids/${id}`} className={styles.backLink}>
        {t("boq.backToBoq")}
      </Link>
      <h1>{t("sections.addTitle")}</h1>
      <p className={styles.subtitle}>{t("sections.addSubtitle")}</p>

      <section className={styles.card}>
        <SectionForm
          action={addSection}
          boqId={boq.id}
          defaultSequence={boq.sections.length + 1}
        />
      </section>
    </main>
  );
}
