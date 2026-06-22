import Link from "next/link";
import { notFound } from "next/navigation";
import { UnitOfMeasureActiveToggle } from "@/app/components/UnitOfMeasureActiveToggle";
import { getUnitOfMeasure, UNIT_CATEGORY_LABELS } from "@/app/lib/api";
import styles from "@/app/page.module.css";
import { setUnitOfMeasureActive } from "../actions";

function formatDate(value?: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString();
}

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
        ← All units of measure
      </Link>
      <h1>{unit.code}</h1>
      <p className={styles.subtitle}>{unit.name}</p>

      <section className={styles.card}>
        <dl className={styles.detailList}>
          <dt>Code</dt>
          <dd>{unit.code}</dd>

          <dt>Name</dt>
          <dd>{unit.name}</dd>

          <dt>Category</dt>
          <dd>{UNIT_CATEGORY_LABELS[unit.category]}</dd>

          <dt>Aliases</dt>
          <dd>{unit.aliases.length > 0 ? unit.aliases.join(", ") : "—"}</dd>

          <dt>Status</dt>
          <dd>
            <span
              className={`${styles.badge} ${
                unit.isActive ? styles.statusActive : styles.statusInactive
              }`}
            >
              {unit.isActive ? "Active" : "Inactive"}
            </span>
          </dd>

          <dt>Created</dt>
          <dd>{formatDate(unit.createdAt)}</dd>
        </dl>
      </section>

      <div className={styles.actions}>
        <Link href={`/units-of-measure/${unit.id}/edit`} className={styles.edit}>
          Edit unit
        </Link>
        <UnitOfMeasureActiveToggle action={setUnitOfMeasureActive} unit={unit} />
      </div>
    </main>
  );
}
