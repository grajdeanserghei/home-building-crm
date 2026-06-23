import Link from "next/link";
import { notFound } from "next/navigation";
import { ContractForm } from "@/app/components/ContractForm";
import { updateContract } from "@/app/contracts/actions";
import { getContract } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
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
      <h1>{t("contracts.editTitle")}</h1>
      <p className={styles.subtitle}>{t("contracts.editSubtitle")}</p>

      <section className={styles.card}>
        <ContractForm
          action={updateContract}
          contract={contract}
          submitLabel={t("common.saveChanges")}
        />
        <Link href={`/contracts/${contract.id}`} className={styles.backLink}>
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
