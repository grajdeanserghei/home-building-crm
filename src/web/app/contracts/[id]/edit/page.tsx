import Link from "next/link";
import { notFound } from "next/navigation";
import { ContractForm } from "@/app/components/ContractForm";
import { updateContract } from "@/app/contracts/actions";
import { getContract } from "@/app/lib/api";
import styles from "@/app/page.module.css";

export default async function EditContractPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const contract = await getContract(id);

  if (!contract) {
    notFound();
  }

  return (
    <main className={styles.main}>
      <h1>Edit contract</h1>
      <p className={styles.subtitle}>
        Update the contract&apos;s header. The work package and accepted bill of
        quantities are fixed; status (signing, completion) has its own controls on the
        contract.
      </p>

      <section className={styles.card}>
        <ContractForm
          action={updateContract}
          contract={contract}
          submitLabel="Save changes"
        />
        <Link href={`/contracts/${contract.id}`} className={styles.backLink}>
          Cancel
        </Link>
      </section>
    </main>
  );
}
