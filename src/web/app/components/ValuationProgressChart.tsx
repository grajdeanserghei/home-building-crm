import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// One plotted point: a snapshot's completion, pre-formatted by the page (the source totals are
// per-currency, so the value label is composed upstream where the currency is known).
export interface ValuationProgressPoint {
  key: string;
  label: string; // the assessment date, formatted
  percentage: number; // 0..100
  valueLabel: string; // completed value, formatted
  percentageLabel: string; // completion %, formatted
}

interface ValuationProgressChartProps {
  points: ValuationProgressPoint[]; // oldest first
}

/**
 * Completed-vs-remaining across snapshots, as a stack of labelled proportion bars (one per site
 * visit, oldest → newest). Each bar's filled portion is the snapshot's completion %. A
 * meter-style rendering (not a plotted chart) keeps it accessible and theme-aware without a
 * charting dependency. Each snapshot uses its own pinned rate — noted in the caller's caption.
 */
export function ValuationProgressChart({ points }: ValuationProgressChartProps) {
  if (points.length === 0) {
    return <p className={styles.muted}>{t("valuation.progress.chartEmpty")}</p>;
  }

  return (
    <div>
      <div className={styles.valuationLegend}>
        <span className={styles.valuationLegendItem}>
          <span
            className={`${styles.valuationSwatch} ${styles.valuationBarCompleted}`}
          />
          {t("valuation.progress.legendCompleted")}
        </span>
        <span className={styles.valuationLegendItem}>
          <span
            className={`${styles.valuationSwatch} ${styles.valuationBarRemaining}`}
          />
          {t("valuation.progress.legendRemaining")}
        </span>
      </div>

      <ul className={styles.valuationChart}>
        {points.map((p) => {
          // Clamp so a malformed 0..100 value never overflows the track.
          const pct = Math.max(0, Math.min(100, p.percentage));
          return (
            <li key={p.key} className={styles.valuationChartRow}>
              <span className={styles.valuationChartLabel}>{p.label}</span>
              <span
                className={styles.valuationBarTrack}
                role="img"
                aria-label={`${p.label}: ${p.percentageLabel}`}
              >
                <span
                  className={styles.valuationBarCompleted}
                  style={{ width: `${pct}%` }}
                />
              </span>
              <span className={styles.valuationChartValue}>
                {p.percentageLabel}
                <span className={styles.muted}>
                  {" · "}
                  {p.valueLabel}
                </span>
              </span>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
