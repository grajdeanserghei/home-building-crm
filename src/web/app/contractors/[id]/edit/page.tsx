import Link from "next/link";
import { notFound } from "next/navigation";
import { ContractorForm } from "@/app/components/ContractorForm";
import { getContractor } from "@/app/lib/api";
import styles from "@/app/page.module.css";
import { updateContractor } from "../../actions";

export default async function EditContractorPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const contractor = await getContractor(id);

  if (!contractor) {
    notFound();
  }

  return (
    <main className={styles.main}>
      <h1>Edit contractor</h1>
      <p className={styles.subtitle}>
        Update the details for &ldquo;{contractor.name}&rdquo;.
      </p>

      <section className={styles.card}>
        <ContractorForm
          action={updateContractor}
          contractor={contractor}
          submitLabel="Save changes"
        />
        <Link href="/contractors" className={styles.backLink}>
          Cancel
        </Link>
      </section>
    </main>
  );
}
