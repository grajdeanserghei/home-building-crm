import Link from "next/link";
import { notFound } from "next/navigation";
import { ContractorForm } from "@/app/components/ContractorForm";
import { getContractor } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
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
      <h1>{t("contractors.editContractor")}</h1>
      <p className={styles.subtitle}>
        {t("contractors.editSubtitleBefore")}&ldquo;{contractor.name}&rdquo;
        {t("contractors.editSubtitleAfter")}
      </p>

      <section className={styles.card}>
        <ContractorForm
          action={updateContractor}
          contractor={contractor}
          submitLabel={t("common.saveChanges")}
        />
        <Link href="/contractors" className={styles.backLink}>
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
