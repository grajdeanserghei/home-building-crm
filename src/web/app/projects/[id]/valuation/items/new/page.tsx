import Link from "next/link";
import { notFound } from "next/navigation";
import { getValuationCatalog } from "@/app/lib/api";
import { ValuationCatalogItemForm } from "@/app/components/ValuationCatalogItemForm";
import { addValuationCatalogItem } from "@/app/projects/[id]/valuation/actions";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function NewValuationItemPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const catalog = await getValuationCatalog(id);

  if (!catalog) {
    notFound();
  }

  const nextSequence =
    catalog.items.reduce((max, i) => Math.max(max, i.sequence), 0) + 1;

  return (
    <main className={styles.main}>
      <h1>{t("valuation.item.addTitle")}</h1>

      <section className={styles.card}>
        <ValuationCatalogItemForm
          action={addValuationCatalogItem}
          projectId={id}
          catalogId={catalog.id}
          currency={catalog.currency}
          defaultSequence={nextSequence}
          submitLabel={t("valuation.item.save")}
        />
        <Link href={`/projects/${id}/valuation`} className={styles.backLink}>
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
