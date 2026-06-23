import Link from "next/link";
import { TradeActiveToggle } from "../components/TradeActiveToggle";
import { TradeForm } from "../components/TradeForm";
import { getTrades, type Trade } from "../lib/api";
import { formatDate } from "../lib/format";
import { t } from "../lib/i18n";
import styles from "../page.module.css";
import { defineTrade, setTradeActive } from "./actions";

export default async function TradesPage() {
  let trades: Trade[] = [];
  let error: string | null = null;

  try {
    trades = await getTrades();
  } catch (e) {
    error = e instanceof Error ? e.message : t("common.unknownError");
  }

  return (
    <main className={styles.main}>
      <h1>{t("trades.title")}</h1>
      <p className={styles.subtitle}>{t("trades.subtitle")}</p>

      <section className={styles.card}>
        <h2>{t("trades.new")}</h2>
        <TradeForm action={defineTrade} submitLabel={t("trades.add")} />
      </section>

      <section className={styles.card}>
        <h2>{t("trades.title")}</h2>
        {error ? (
          <p className={styles.error}>{t("common.apiError", { error })}</p>
        ) : trades.length === 0 ? (
          <p>{t("trades.empty")}</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t("common.name")}</th>
                <th>{t("trades.code")}</th>
                <th>{t("common.status")}</th>
                <th>{t("common.created")}</th>
                <th aria-label={t("common.actions")} />
              </tr>
            </thead>
            <tbody>
              {trades.map((tr) => (
                <tr key={tr.id}>
                  <td>
                    <Link href={`/trades/${tr.id}`} className={styles.nameLink}>
                      <strong>{tr.name}</strong>
                    </Link>
                  </td>
                  <td>
                    {tr.code ? tr.code : <span className={styles.muted}>—</span>}
                  </td>
                  <td>
                    <span
                      className={`${styles.badge} ${
                        tr.isActive ? styles.statusActive : styles.statusInactive
                      }`}
                    >
                      {tr.isActive ? t("trades.active") : t("trades.inactive")}
                    </span>
                  </td>
                  <td>{formatDate(tr.createdAt)}</td>
                  <td>
                    <div className={styles.actions}>
                      <Link
                        href={`/trades/${tr.id}/edit`}
                        className={styles.edit}
                      >
                        {t("common.edit")}
                      </Link>
                      <TradeActiveToggle action={setTradeActive} trade={tr} />
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <Link href="/" className={styles.backLink}>
        {t("trades.backToProjects")}
      </Link>
    </main>
  );
}
