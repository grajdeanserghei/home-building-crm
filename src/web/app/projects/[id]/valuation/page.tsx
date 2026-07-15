import Link from "next/link";
import { notFound } from "next/navigation";
import {
  getProject,
  getValuationCatalog,
  VALUATION_CATALOG_STATUS_LABELS,
  VALUATION_METHOD_LABELS,
  type ValuationCatalogItem,
} from "@/app/lib/api";
import { getDisplayCurrency, getDisplayRate } from "@/app/lib/display-currency";
import { displayMoney, formatNumber, formatPercent } from "@/app/lib/format";
import { ConfirmDeleteButton } from "@/app/components/ConfirmDeleteButton";
import { SubmitButton } from "@/app/components/SubmitButton";
import {
  activateValuationCatalog,
  changeValuationVat,
  deactivateValuationCatalogItem,
} from "@/app/projects/[id]/valuation/actions";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// The count of BoQ mappings on an item, as a Romanian label ("— nemapat —" / "1 mapare" / "N mapări").
function mappingLabel(item: ValuationCatalogItem): string {
  const n = item.links.length;
  if (n === 0) return t("valuation.item.mappingNone");
  if (n === 1) return t("valuation.item.mappingCountOne");
  return t("valuation.item.mappingCount", { count: String(n) });
}

export default async function ValuationHubPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const [project, catalog, displayCurrency, rate] = await Promise.all([
    getProject(id),
    getValuationCatalog(id),
    getDisplayCurrency(),
    getDisplayRate(),
  ]);

  if (!project) {
    notFound();
  }

  const money = (m: Parameters<typeof displayMoney>[0]) =>
    displayMoney(m, displayCurrency, rate);

  return (
    <main className={styles.main}>
      <Link href={`/projects/${id}`} className={styles.backLink}>
        {t("valuation.backToProject")}
      </Link>
      <h1>{t("valuation.title", { name: project.name })}</h1>
      <p className={styles.subtitle}>{t("valuation.subtitle")}</p>

      {!catalog ? (
        <section className={styles.card}>
          <p className={styles.muted}>{t("valuation.empty.body")}</p>
          <div className={styles.actions}>
            <Link
              href={`/projects/${id}/valuation/new`}
              className={styles.primaryButton}
            >
              {t("valuation.create")}
            </Link>
          </div>
        </section>
      ) : (
        <>
          <section className={styles.card}>
            <dl className={styles.detailList}>
              <dt>{t("valuation.header.method")}</dt>
              <dd>{VALUATION_METHOD_LABELS[catalog.method]}</dd>
              <dt>{t("valuation.header.catalogReference")}</dt>
              <dd>{catalog.catalogReference}</dd>
              <dt>{t("valuation.header.status")}</dt>
              <dd>
                <span
                  className={`${styles.badge} ${styles[`status${catalog.status}`]}`}
                >
                  {VALUATION_CATALOG_STATUS_LABELS[catalog.status]}
                </span>
              </dd>
              <dt>{t("valuation.header.builtArea")}</dt>
              <dd>{formatNumber(catalog.builtArea)}</dd>
              <dt>{t("valuation.header.grossFloorArea")}</dt>
              <dd>{formatNumber(catalog.grossFloorArea)}</dd>
              <dt>{t("valuation.header.usableArea")}</dt>
              <dd>{formatNumber(catalog.usableArea)}</dd>
              <dt>{t("valuation.header.ownRegieAdjustment")}</dt>
              <dd>{formatPercent(catalog.ownRegieAdjustment * 100)}</dd>
              <dt>{t("valuation.header.vatRate")}</dt>
              <dd>{formatPercent(catalog.vatRatePercentage)}</dd>
              <dt>{t("valuation.header.currency")}</dt>
              <dd>{catalog.currency}</dd>
            </dl>

            <div className={styles.actions}>
              <Link
                href={`/projects/${id}/valuation/edit`}
                className={styles.edit}
              >
                {t("valuation.actions.edit")}
              </Link>
              {catalog.status === "Draft" ? (
                <form action={activateValuationCatalog}>
                  <input type="hidden" name="projectId" value={id} />
                  <input type="hidden" name="catalogId" value={catalog.id} />
                  <SubmitButton
                    label={t("valuation.actions.activate")}
                    className={styles.edit}
                  />
                </form>
              ) : null}
            </div>

            {/* VAT change is its own control: it triggers a write-time recompute of every item's
                gross total — but never touches existing snapshots (the note states the asymmetry). */}
            <form action={changeValuationVat} className={styles.boqMapForm}>
              <input type="hidden" name="projectId" value={id} />
              <input type="hidden" name="catalogId" value={catalog.id} />
              <label className={styles.muted}>{t("valuation.header.vatRate")}</label>
              <input
                name="vatRatePercentage"
                type="number"
                min={0}
                step="0.01"
                defaultValue={catalog.vatRatePercentage}
                aria-label={t("valuation.header.vatRate")}
              />
              <SubmitButton
                label={t("valuation.vat.submit")}
                className={styles.edit}
              />
            </form>
            <p className={styles.muted}>{t("valuation.vat.note")}</p>

            <div className={styles.linkRow}>
              <Link
                href={`/projects/${id}/valuation/vs-boq`}
                className={styles.edit}
              >
                {t("valuation.subnav.vsBoq")} →
              </Link>
              <Link
                href={`/projects/${id}/valuation/progress`}
                className={styles.edit}
              >
                {t("valuation.subnav.progress")} →
              </Link>
              <Link
                href={`/projects/${id}/valuation/snapshots`}
                className={styles.edit}
              >
                {t("valuation.subnav.snapshots")} →
              </Link>
            </div>
          </section>

          <section className={styles.card}>
            <h2>{t("valuation.items.title")}</h2>
            {catalog.items.length === 0 ? (
              <p className={styles.muted}>{t("valuation.items.empty")}</p>
            ) : (
              <div className={styles.tableWrap}>
                <table className={styles.table}>
                  <thead>
                    <tr>
                      <th>{t("valuation.item.col.printedNumber")}</th>
                      <th>{t("valuation.item.col.name")}</th>
                      <th>{t("valuation.item.col.source")}</th>
                      <th>{t("valuation.item.col.unit")}</th>
                      <th>{t("valuation.item.col.unitCost")}</th>
                      <th>{t("valuation.item.col.weight")}</th>
                      <th>{t("valuation.item.col.costNet")}</th>
                      <th>{t("valuation.item.col.costGross")}</th>
                      <th>{t("valuation.item.col.mapping")}</th>
                      <th aria-label={t("common.actions")} />
                    </tr>
                  </thead>
                  <tbody>
                    {[...catalog.items]
                      .sort((a, b) => a.sequence - b.sequence)
                      .map((item) => (
                        <tr key={item.id}>
                          <td>{item.printedNumber}</td>
                          <td>
                            {item.isActive ? (
                              <strong>{item.name}</strong>
                            ) : (
                              <span className={styles.muted}>
                                {item.name}{" "}
                                <span
                                  className={`${styles.badge} ${styles.statusInactive}`}
                                >
                                  {t("valuation.item.retired")}
                                </span>
                              </span>
                            )}
                          </td>
                          <td>{item.catalogSource}</td>
                          <td>{item.unit}</td>
                          <td>{money(item.unitCostPerBuiltArea)}</td>
                          <td>{formatNumber(item.costWeight)}</td>
                          <td>{money(item.totalCostWithoutVat)}</td>
                          <td>{money(item.totalCostWithVat)}</td>
                          <td className={styles.muted}>{mappingLabel(item)}</td>
                          <td>
                            {item.isActive ? (
                              <div className={styles.actions}>
                                <Link
                                  href={`/projects/${id}/valuation/items/${item.id}/edit`}
                                  className={styles.edit}
                                >
                                  {t("valuation.item.revise")}
                                </Link>
                                <ConfirmDeleteButton
                                  action={deactivateValuationCatalogItem}
                                  fields={{
                                    projectId: id,
                                    catalogId: catalog.id,
                                    itemId: item.id,
                                  }}
                                  title={t("valuation.item.deactivateTitle")}
                                  bodyTemplate={t("valuation.item.deactivateBody")}
                                  name={item.name}
                                  triggerLabel={t("valuation.item.deactivate")}
                                  confirmLabel={t("valuation.item.deactivate")}
                                />
                              </div>
                            ) : null}
                          </td>
                        </tr>
                      ))}
                  </tbody>
                </table>
              </div>
            )}
            <p>
              <Link
                href={`/projects/${id}/valuation/items/new`}
                className={styles.primaryButton}
              >
                {t("valuation.items.add")}
              </Link>
            </p>
          </section>
        </>
      )}
    </main>
  );
}
