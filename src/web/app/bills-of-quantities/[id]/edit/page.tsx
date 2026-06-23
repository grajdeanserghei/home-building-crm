import Link from "next/link";
import { notFound } from "next/navigation";
import { BillOfQuantitiesForm } from "@/app/components/BillOfQuantitiesForm";
import { updateBoq } from "@/app/bills-of-quantities/actions";
import { getBillOfQuantities } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function EditBillOfQuantitiesPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const boq = await getBillOfQuantities(id);

  if (!boq) {
    notFound();
  }

  return (
    <main className={styles.main}>
      <Link href={`/bills-of-quantities/${boq.id}`} className={styles.backLink}>
        {t("boq.backToBoq")}
      </Link>
      <h1>{t("boq.editTitle", { version: boq.version })}</h1>
      <p className={styles.subtitle}>{t("boq.editSubtitle")}</p>

      <section className={styles.card}>
        <BillOfQuantitiesForm
          action={updateBoq}
          boq={boq}
          submitLabel={t("common.saveChanges")}
        />
      </section>
    </main>
  );
}
