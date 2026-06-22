import Link from "next/link";
import { UnitOfMeasureActiveToggle } from "../components/UnitOfMeasureActiveToggle";
import { UnitOfMeasureForm } from "../components/UnitOfMeasureForm";
import {
  getUnitsOfMeasure,
  UNIT_CATEGORY_LABELS,
  type UnitOfMeasure,
} from "../lib/api";
import styles from "../page.module.css";
import { defineUnitOfMeasure, setUnitOfMeasureActive } from "./actions";

function formatDate(value?: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString();
}

export default async function UnitsOfMeasurePage() {
  let units: UnitOfMeasure[] = [];
  let error: string | null = null;

  try {
    units = await getUnitsOfMeasure();
  } catch (e) {
    error = e instanceof Error ? e.message : "Unknown error";
  }

  return (
    <main className={styles.main}>
      <h1>Units of measure</h1>
      <p className={styles.subtitle}>
        Canonical units used to quantify bill-of-quantities lines.
      </p>

      <section className={styles.card}>
        <h2>New unit</h2>
        <UnitOfMeasureForm action={defineUnitOfMeasure} submitLabel="Add unit" />
      </section>

      <section className={styles.card}>
        <h2>All units</h2>
        {error ? (
          <p className={styles.error}>Could not reach the API: {error}</p>
        ) : units.length === 0 ? (
          <p>No units yet. Add your first one above.</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>Code</th>
                <th>Name</th>
                <th>Category</th>
                <th>Aliases</th>
                <th>Status</th>
                <th>Created</th>
                <th aria-label="actions" />
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
                      {u.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  <td>{formatDate(u.createdAt)}</td>
                  <td>
                    <div className={styles.actions}>
                      <Link
                        href={`/units-of-measure/${u.id}/edit`}
                        className={styles.edit}
                      >
                        Edit
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
        ← Projects
      </Link>
    </main>
  );
}
