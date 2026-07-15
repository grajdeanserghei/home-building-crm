import Link from "next/link";
import { notFound } from "next/navigation";
import {
  getConstructionValuations,
  getProject,
  getValuationCatalog,
} from "@/app/lib/api";
import { formatDate, formatPercent } from "@/app/lib/format";
import { snapshotCompletion } from "@/app/lib/valuation";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function ValuationSnapshotsPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const [project, catalog] = await Promise.all([
    getProject(id),
    getValuationCatalog(id),
  ]);

  if (!project) {
    notFound();
  }

  // Snapshots are catalog-scoped; with no catalog there are none to list.
  const snapshots = catalog ? await getConstructionValuations(catalog.id) : [];

  return (
    <main className={styles.main}>
      <Link href={`/projects/${id}/valuation`} className={styles.backLink}>
        {t("valuation.backToProject")}
      </Link>
      <h1>{t("valuation.snapshot.listTitle", { name: project.name })}</h1>
      <p className={styles.subtitle}>{t("valuation.snapshot.subtitle")}</p>

      <section className={styles.card}>
        {snapshots.length === 0 ? (
          <p className={styles.muted}>{t("valuation.snapshot.empty")}</p>
        ) : (
          <div className={styles.tableWrap}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>{t("valuation.snapshot.col.assessedOn")}</th>
                  <th>{t("valuation.snapshot.col.appraiser")}</th>
                  <th>{t("valuation.snapshot.col.items")}</th>
                  <th>{t("valuation.snapshot.col.completion")}</th>
                </tr>
              </thead>
              <tbody>
                {snapshots.map((s) => (
                  <tr key={s.id}>
                    <td>
                      <Link
                        href={`/projects/${id}/valuation/snapshots/${s.id}`}
                        className={styles.nameLink}
                      >
                        <strong>{formatDate(s.assessedOn)}</strong>
                      </Link>
                    </td>
                    <td>{s.appraiser || "—"}</td>
                    <td>{s.items.length}</td>
                    <td>{formatPercent(snapshotCompletion(s.totals).percentage)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}
