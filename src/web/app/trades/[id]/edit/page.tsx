import Link from "next/link";
import { notFound } from "next/navigation";
import { TradeForm } from "@/app/components/TradeForm";
import { getTrade } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";
import { updateTrade } from "../../actions";

export default async function EditTradePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const trade = await getTrade(id);

  if (!trade) {
    notFound();
  }

  return (
    <main className={styles.main}>
      <h1>{t("trades.editTitle")}</h1>
      <p className={styles.subtitle}>
        {t("trades.editSubtitle", { name: trade.name })}
      </p>

      <section className={styles.card}>
        <TradeForm
          action={updateTrade}
          trade={trade}
          submitLabel={t("common.saveChanges")}
        />
        <Link href="/trades" className={styles.backLink}>
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
