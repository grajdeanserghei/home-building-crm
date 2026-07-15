import Link from "next/link";
import { notFound } from "next/navigation";
import { getProject } from "@/app/lib/api";
import { ValuationCatalogForm } from "@/app/components/ValuationCatalogForm";
import { createValuationCatalog } from "@/app/projects/[id]/valuation/actions";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function NewValuationCatalogPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const project = await getProject(id);

  if (!project) {
    notFound();
  }

  return (
    <main className={styles.main}>
      <h1>{t("valuation.newTitle")}</h1>
      <p className={styles.subtitle}>{t("valuation.newSubtitle")}</p>

      <section className={styles.card}>
        <ValuationCatalogForm
          action={createValuationCatalog}
          projectId={id}
          submitLabel={t("valuation.create")}
        />
        <Link href={`/projects/${id}/valuation`} className={styles.backLink}>
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
