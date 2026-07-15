import Link from "next/link";
import { notFound } from "next/navigation";
import { getValuationCatalog } from "@/app/lib/api";
import { ValuationCatalogItemForm } from "@/app/components/ValuationCatalogItemForm";
import { reviseValuationCatalogItem } from "@/app/projects/[id]/valuation/actions";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function EditValuationItemPage({
  params,
}: {
  params: Promise<{ id: string; itemId: string }>;
}) {
  const { id, itemId } = await params;
  const catalog = await getValuationCatalog(id);
  const item = catalog?.items.find((i) => i.id === itemId);

  if (!catalog || !item) {
    notFound();
  }

  return (
    <main className={styles.main}>
      <h1>{t("valuation.item.editTitle")}</h1>

      <section className={styles.card}>
        <ValuationCatalogItemForm
          action={reviseValuationCatalogItem}
          projectId={id}
          catalogId={catalog.id}
          currency={catalog.currency}
          item={item}
          submitLabel={t("valuation.item.save")}
        />
        <Link href={`/projects/${id}/valuation`} className={styles.backLink}>
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
