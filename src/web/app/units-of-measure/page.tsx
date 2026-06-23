import Link from "next/link";
import { UnitOfMeasureActiveToggle } from "../components/UnitOfMeasureActiveToggle";
import { UnitOfMeasureForm } from "../components/UnitOfMeasureForm";
import {
  getUnitsOfMeasure,
  UNIT_CATEGORY_LABELS,
  type UnitOfMeasure,
} from "../lib/api";
import { formatDate } from "../lib/format";
import { t } from "../lib/i18n";
import styles from "../page.module.css";
import { defineUnitOfMeasure, setUnitOfMeasureActive } from "./actions";

export default async function UnitsOfMeasurePage() {
  let units: UnitOfMeasure[] = [];
  let error: string | null = null;

  try {
    units = await getUnitsOfMeasure();
  } catch (e) {
    error = e instanceof Error ? e.message : t("common.unknownError");
  }

  return (
    <main className={styles.main}>
      <h1>{t("unitsOfMeasure.title")}</h1>
      <p className={styles.subtitle}>{t("unitsOfMeasure.subtitle")}</p>

      <section className={styles.card}>
        <h2>{t("unitsOfMeasure.new")}</h2>
        <UnitOfMeasureForm
          action={defineUnitOfMeasure}
          submitLabel={t("unitsOfMeasure.add")}
        />
      </section>

      <section className={styles.card}>
        <h2>{t("unitsOfMeasure.title")}</h2>
        {error ? (
          <p className={styles.error}>{t("common.apiError", { error })}</p>
        ) : units.length === 0 ? (
          <p>{t("unitsOfMeasure.empty")}</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t("unitsOfMeasure.code")}</th>
                <th>{t("common.name")}</th>
                <th>{t("unitsOfMeasure.category")}</th>
                <th>{t("unitsOfMeasure.aliases")}</th>
                <th>{t("common.status")}</th>
                <th>{t("common.created")}</th>
                <th aria-label={t("common.actions")} />
              </tr>
            </thead>
            <tbody>
              {units.map((u) => (
                <tr key={u.id}>
                  <td>
                    <Link
                      href={`/units-of-measure/${u.id}`}
                      className={styles.nameLink}
                    >
                      <strong>{u.code}</strong>
                    </Link>
                  </td>
                  <td>{u.name}</td>
                  <td>{UNIT_CATEGORY_LABELS[u.category]}</td>
                  <td>
                    {u.aliases.length > 0 ? (
                      u.aliases.join(", ")
                    ) : (
                      <span className={styles.muted}>—</span>
                    )}
                  </td>
                  <td>
                    <span
                      className={`${styles.badge} ${
                        u.isActive ? styles.statusActive : styles.statusInactive
                      }`}
                    >
                      {u.isActive
                        ? t("unitsOfMeasure.active")
                        : t("unitsOfMeasure.inactive")}
                    </span>
                  </td>
                  <td>{formatDate(u.createdAt)}</td>
                  <td>
                    <div className={styles.actions}>
                      <Link
                        href={`/units-of-measure/${u.id}/edit`}
                        className={styles.edit}
                      >
                        {t("common.edit")}
                      </Link>
                      <UnitOfMeasureActiveToggle
                        action={setUnitOfMeasureActive}
                        unit={u}
                      />
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <Link href="/" className={styles.backLink}>
        {t("unitsOfMeasure.backToProjects")}
      </Link>
    </main>
  );
}
