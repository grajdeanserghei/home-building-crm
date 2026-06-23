import Link from "next/link";
import { notFound } from "next/navigation";
import { UnitOfMeasureForm } from "@/app/components/UnitOfMeasureForm";
import { getUnitOfMeasure } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";
import { updateUnitOfMeasure } from "../../actions";

export default async function EditUnitOfMeasurePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const unit = await getUnitOfMeasure(id);

  if (!unit) {
    notFound();
  }

  return (
    <main className={styles.main}>
      <h1>{t("unitsOfMeasure.editTitle")}</h1>
      <p className={styles.subtitle}>
        {t("unitsOfMeasure.editSubtitle", { code: unit.code })}
      </p>

      <section className={styles.card}>
        <UnitOfMeasureForm
          action={updateUnitOfMeasure}
          unit={unit}
          submitLabel={t("common.saveChanges")}
        />
        <Link href="/units-of-measure" className={styles.backLink}>
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
