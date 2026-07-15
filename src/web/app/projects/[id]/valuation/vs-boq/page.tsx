import Link from "next/link";
import { getProject, getValuationComparison } from "@/app/lib/api";
import { getDisplayCurrency } from "@/app/lib/display-currency";
import { ValuationComparisonTable } from "@/app/components/ValuationComparisonTable";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function ValuationVsBoqPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const [project, comparison, displayCurrency] = await Promise.all([
    getProject(id),
    getValuationComparison(id),
    getDisplayCurrency(),
  ]);

  const projectName = project?.name ?? "";

  // No catalog for this project yet — invite creating one, mirroring the hub's empty state.
  if (!comparison) {
    return (
      <main className={styles.main}>
        <Link href={`/projects/${id}/valuation`} className={styles.backLink}>
          {t("valuation.backToProject")}
        </Link>
        <h1>{t("valuation.subnav.vsBoq")}</h1>
        <section className={styles.card}>
          <p className={styles.muted}>{t("valuation.empty.body")}</p>
          <div className={styles.actions}>
            <Link
              href={`/projects/${id}/valuation`}
              className={styles.primaryButton}
            >
              {t("valuation.link")} →
            </Link>
          </div>
        </section>
      </main>
    );
  }

  return (
    <main className={styles.main}>
      <Link href={`/projects/${id}/valuation`} className={styles.backLink}>
        {t("valuation.backToProject")}
      </Link>
      <h1>{t("valuation.vsBoq.title", { name: projectName })}</h1>
      <p className={styles.subtitle}>{t("valuation.vsBoq.subtitle")}</p>

      <ValuationComparisonTable
        comparison={comparison}
        displayCurrency={displayCurrency}
      />
    </main>
  );
}
