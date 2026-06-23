import Link from "next/link";
import { notFound } from "next/navigation";
import { TradeActiveToggle } from "@/app/components/TradeActiveToggle";
import { getTrade } from "@/app/lib/api";
import { formatDate } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";
import { setTradeActive } from "../actions";

export default async function TradeDetailPage({
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
      <Link href="/trades" className={styles.backLink}>
        {t("trades.backToAll")}
      </Link>
      <h1>{trade.name}</h1>

      <section className={styles.card}>
        <dl className={styles.detailList}>
          <dt>{t("common.name")}</dt>
          <dd>{trade.name}</dd>

          <dt>{t("trades.code")}</dt>
          <dd>{trade.code || "—"}</dd>

          <dt>{t("common.status")}</dt>
          <dd>
            <span
              className={`${styles.badge} ${
                trade.isActive ? styles.statusActive : styles.statusInactive
              }`}
            >
              {trade.isActive ? t("trades.active") : t("trades.inactive")}
            </span>
          </dd>

          <dt>{t("common.created")}</dt>
          <dd>{formatDate(trade.createdAt)}</dd>
        </dl>
      </section>

      <div className={styles.actions}>
        <Link href={`/trades/${trade.id}/edit`} className={styles.edit}>
          {t("trades.editTrade")}
        </Link>
        <TradeActiveToggle action={setTradeActive} trade={trade} />
      </div>
    </main>
  );
}
