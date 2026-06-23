import Link from "next/link";
import { notFound } from "next/navigation";
import { UnitOfMeasureActiveToggle } from "@/app/components/UnitOfMeasureActiveToggle";
import { getUnitOfMeasure, UNIT_CATEGORY_LABELS } from "@/app/lib/api";
import { formatDate } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";
import { setUnitOfMeasureActive } from "../actions";

export default async function UnitOfMeasureDetailPage({
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
      <Link href="/units-of-measure" className={styles.backLink}>
        {t("unitsOfMeasure.backToAll")}
      </Link>
      <h1>{unit.code}</h1>
      <p className={styles.subtitle}>{unit.name}</p>

      <section className={styles.card}>
        <dl className={styles.detailList}>
          <dt>{t("unitsOfMeasure.code")}</dt>
          <dd>{unit.code}</dd>

          <dt>{t("common.name")}</dt>
          <dd>{unit.name}</dd>

          <dt>{t("unitsOfMeasure.category")}</dt>
          <dd>{UNIT_CATEGORY_LABELS[unit.category]}</dd>

          <dt>{t("unitsOfMeasure.aliases")}</dt>
          <dd>{unit.aliases.length > 0 ? unit.aliases.join(", ") : "—"}</dd>

          <dt>{t("common.status")}</dt>
          <dd>
            <span
              className={`${styles.badge} ${
                unit.isActive ? styles.statusActive : styles.statusInactive
              }`}
            >
              {unit.isActive
                ? t("unitsOfMeasure.active")
                : t("unitsOfMeasure.inactive")}
            </span>
          </dd>

          <dt>{t("common.created")}</dt>
          <dd>{formatDate(unit.createdAt)}</dd>
        </dl>
      </section>

      <div className={styles.actions}>
        <Link href={`/units-of-measure/${unit.id}/edit`} className={styles.edit}>
          {t("unitsOfMeasure.editUnit")}
        </Link>
        <UnitOfMeasureActiveToggle action={setUnitOfMeasureActive} unit={unit} />
      </div>
    </main>
  );
}
