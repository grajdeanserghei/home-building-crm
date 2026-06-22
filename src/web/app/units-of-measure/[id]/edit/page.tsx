import Link from "next/link";
import { notFound } from "next/navigation";
import { UnitOfMeasureForm } from "@/app/components/UnitOfMeasureForm";
import { getUnitOfMeasure } from "@/app/lib/api";
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
      <h1>Edit unit of measure</h1>
      <p className={styles.subtitle}>
        Update the details for &ldquo;{unit.code}&rdquo;. The code is fixed and
        cannot be changed.
      </p>

      <section className={styles.card}>
        <UnitOfMeasureForm
          action={updateUnitOfMeasure}
          unit={unit}
          submitLabel="Save changes"
        />
        <Link href="/units-of-measure" className={styles.backLink}>
          Cancel
        </Link>
      </section>
    </main>
  );
}
