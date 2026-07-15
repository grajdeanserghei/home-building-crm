import Link from "next/link";
import { notFound } from "next/navigation";
import { getValuationCatalog } from "@/app/lib/api";
import { ValuationCatalogForm } from "@/app/components/ValuationCatalogForm";
import { updateValuationCatalog } from "@/app/projects/[id]/valuation/actions";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function EditValuationCatalogPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const catalog = await getValuationCatalog(id);

  if (!catalog) {
    notFound();
  }

  return (
    <main className={styles.main}>
      <h1>{t("valuation.editTitle")}</h1>
      <p className={styles.subtitle}>{t("valuation.editSubtitle")}</p>

      <section className={styles.card}>
        <ValuationCatalogForm
          action={updateValuationCatalog}
          projectId={id}
          catalog={catalog}
          submitLabel={t("valuation.form.save")}
        />
        <Link href={`/projects/${id}/valuation`} className={styles.backLink}>
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
