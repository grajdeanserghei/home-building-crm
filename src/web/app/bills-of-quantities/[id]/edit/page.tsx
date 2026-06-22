import Link from "next/link";
import { notFound } from "next/navigation";
import { BillOfQuantitiesForm } from "@/app/components/BillOfQuantitiesForm";
import { updateBoq } from "@/app/bills-of-quantities/actions";
import { getBillOfQuantities } from "@/app/lib/api";
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
        ← Back to BoQ
      </Link>
      <h1>Edit BoQ v{boq.version}</h1>
      <p className={styles.subtitle}>
        Update the header. The pricing currency and version are fixed; sections and line
        items are edited on the BoQ page.
      </p>

      <section className={styles.card}>
        <BillOfQuantitiesForm
          action={updateBoq}
          boq={boq}
          submitLabel="Save changes"
        />
      </section>
    </main>
  );
}
