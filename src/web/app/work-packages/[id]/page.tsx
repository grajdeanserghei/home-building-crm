import Link from "next/link";
import { notFound } from "next/navigation";
import { TradeChips } from "@/app/components/TradeChips";
import {
  addRequiredTrade,
  deleteWorkPackage,
  removeRequiredTrade,
  removeScopeItem,
} from "@/app/work-packages/actions";
import {
  BID_STATUS_LABELS,
  getBids,
  getContractors,
  getTrades,
  getWorkPackage,
  SCOPE_ITEM_REQUIREMENT_LABELS,
  WORK_PACKAGE_STATUS_LABELS,
  type Bid,
  type Contractor,
  type Trade,
  type WorkPackageStatus,
} from "@/app/lib/api";
import { formatDate } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// Whether the package has any onward lifecycle transitions. Awarded is reached only through
// the award flow (selecting a bid, creating a contract); Completed and Cancelled are terminal.
// The targets themselves are computed on the dedicated status route.
function canChangeStatus(current: WorkPackageStatus): boolean {
  return (
    current === "Defined" ||
    current === "OpenForBids" ||
    current === "Awarded" ||
    current === "InProgress"
  );
}

// Read-first detail page: it shows the package, its required trades, scope items and bids,
// with no inline create/edit forms. Every mutation (defining scope, opening a bid, changing
// status, editing) is a deliberate step away on its own route — the one exception is the
// incremental required-trades control, mirroring the contractor detail page.
export default async function WorkPackageDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const workPackage = await getWorkPackage(id);

  if (!workPackage) {
    notFound();
  }

  let bids: Bid[] = [];
  let contractors: Contractor[] = [];
  let trades: Trade[] = [];
  let error: string | null = null;

  try {
    [bids, contractors, trades] = await Promise.all([
      getBids(id),
      getContractors(),
      getTrades(),
    ]);
  } catch (e) {
    error = e instanceof Error ? e.message : t("common.unknownError");
  }

  // Resolve the package's required-trade ids to names (all trades, so a retired-but-still-required
  // trade still shows a label) and offer the active, not-yet-required ones in the add control.
  const tradeNameById = new Map(trades.map((tr) => [tr.id, tr.name]));
  const assignedTrades = workPackage.requiredTradeIds.map((tid) => ({
    id: tid,
    name: tradeNameById.get(tid) ?? tid,
  }));
  const requiredIds = new Set(workPackage.requiredTradeIds);
  const availableTrades = trades
    .filter((tr) => tr.isActive && !requiredIds.has(tr.id))
    .map((tr) => ({ id: tr.id, name: tr.name }));

  // Map contractor id → name for the bids table.
  const contractorName = new Map(contractors.map((c) => [c.id, c.name]));

  const scopeItems = workPackage.scopeItems;

  return (
    <main className={styles.main}>
      <Link href={`/projects/${workPackage.projectId}`} className={styles.backLink}>
        {t("workPackages.backToProject")}
      </Link>

      <div className={styles.toolbar}>
        <div>
          <h1>{workPackage.name}</h1>
          <p className={styles.subtitle}>
            {workPackage.description || t("workPackages.detailSubtitle")}
            {" · "}
            <span
              className={`${styles.badge} ${styles[`status${workPackage.status}`]}`}
            >
              {WORK_PACKAGE_STATUS_LABELS[workPackage.status]}
            </span>
          </p>
        </div>
        <Link
          href={`/work-packages/${workPackage.id}/bids/new`}
          className={styles.primaryButton}
        >
          {t("workPackages.openBid")}
        </Link>
      </div>

      <section className={styles.card}>
        <div className={styles.cardHeader}>
          <h2>{t("workPackages.bidsTitle")}</h2>
          <Link
            href={`/work-packages/${workPackage.id}/bids/new`}
            className={styles.edit}
          >
            {t("workPackages.openBid")}
          </Link>
        </div>
        {error ? (
          <p className={styles.error}>{t("common.apiError", { error })}</p>
        ) : bids.length === 0 ? (
          <p>{t("workPackages.bidsEmpty")}</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t("workPackages.bidContractor")}</th>
                <th>{t("common.status")}</th>
                <th>{t("workPackages.bidFirstContact")}</th>
                <th>{t("common.notes")}</th>
                <th aria-label={t("common.actions")} />
              </tr>
            </thead>
            <tbody>
              {bids.map((b) => (
                <tr key={b.id}>
                  <td>
                    <Link href={`/bids/${b.id}`} className={styles.nameLink}>
                      <strong>
                        {contractorName.get(b.contractorId) ??
                          t("workPackages.unknownContractor")}
                      </strong>
                    </Link>
                    {b.summary ? (
                      <div className={styles.muted}>{b.summary}</div>
                    ) : null}
                  </td>
                  <td>
                    <span
                      className={`${styles.badge} ${styles[`status${b.status}`]}`}
                    >
                      {BID_STATUS_LABELS[b.status]}
                    </span>
                  </td>
                  <td>{formatDate(b.firstContactedOn)}</td>
                  <td>{b.notes.length}</td>
                  <td>
                    <div className={styles.actions}>
                      <Link href={`/bids/${b.id}`} className={styles.edit}>
                        {t("workPackages.bidView")}
                      </Link>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      {workPackage.awardedContractId ? (
        <section className={styles.card}>
          <h2>{t("workPackages.contractTitle")}</h2>
          <p>
            {t("workPackages.awardedNotice")}{" "}
            <Link
              href={`/contracts/${workPackage.awardedContractId}`}
              className={styles.nameLink}
            >
              {t("workPackages.viewContract")}
            </Link>
          </p>
        </section>
      ) : null}

      <section className={styles.card}>
        <dl className={styles.detailList}>
          <dt>{t("common.status")}</dt>
          <dd>{WORK_PACKAGE_STATUS_LABELS[workPackage.status]}</dd>
          <dt>{t("workPackages.orderPlaceholder")}</dt>
          <dd>{workPackage.sequence}</dd>
          <dt>{t("workPackages.plannedStart")}</dt>
          <dd>{formatDate(workPackage.plannedStartDate)}</dd>
          <dt>{t("workPackages.plannedEnd")}</dt>
          <dd>{formatDate(workPackage.plannedEndDate)}</dd>
          <dt>{t("common.created")}</dt>
          <dd>{formatDate(workPackage.createdAt)}</dd>
        </dl>
        <div className={styles.actions}>
          {canChangeStatus(workPackage.status) ? (
            <Link
              href={`/work-packages/${workPackage.id}/status`}
              className={styles.edit}
            >
              {t("workPackages.changeStatusTitle")}
            </Link>
          ) : null}
          <Link
            href={`/work-packages/${workPackage.id}/edit`}
            className={styles.edit}
          >
            {t("common.edit")}
          </Link>
          <form action={deleteWorkPackage}>
            <input type="hidden" name="id" value={workPackage.id} />
            <input type="hidden" name="projectId" value={workPackage.projectId} />
            <button type="submit" className={styles.delete}>
              {t("common.delete")}
            </button>
          </form>
        </div>
        <p className={styles.muted}>{t("workPackages.awardingHint")}</p>
      </section>

      <section className={styles.card}>
        <h2>{t("workPackages.requiredTrades")}</h2>
        <TradeChips
          assigned={assignedTrades}
          available={availableTrades}
          ownerFieldName="workPackageId"
          ownerId={workPackage.id}
          addAction={addRequiredTrade}
          removeAction={removeRequiredTrade}
          emptyLabel={t("workPackages.requiredTradesEmpty")}
          addLabel={t("workPackages.addRequiredTrade")}
          selectPlaceholder={t("workPackages.selectTrade")}
          allAssignedLabel={t("workPackages.allTradesRequired")}
          removeAriaLabel={(name) => t("workPackages.removeTradeAria", { name })}
        />
      </section>

      <section className={styles.card}>
        <div className={styles.cardHeader}>
          <h2>{t("scopeItems.title")}</h2>
          <Link
            href={`/work-packages/${workPackage.id}/scope-items/new`}
            className={styles.edit}
          >
            {t("scopeItems.add")}
          </Link>
        </div>
        <p className={styles.muted}>{t("scopeItems.subtitle")}</p>
        {scopeItems.length === 0 ? (
          <p>{t("scopeItems.empty")}</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>#</th>
                <th>{t("common.name")}</th>
                <th>{t("scopeItems.requirement")}</th>
                <th aria-label={t("common.actions")} />
              </tr>
            </thead>
            <tbody>
              {scopeItems.map((si) => (
                <tr key={si.id}>
                  <td>{si.sequence}</td>
                  <td>
                    <strong>{si.name}</strong>
                    {si.description ? (
                      <div className={styles.muted}>{si.description}</div>
                    ) : null}
                  </td>
                  <td>
                    <span
                      className={`${styles.badge} ${
                        styles[`requirement${si.requirement}`]
                      }`}
                    >
                      {SCOPE_ITEM_REQUIREMENT_LABELS[si.requirement]}
                    </span>
                  </td>
                  <td>
                    <div className={styles.actions}>
                      <form action={removeScopeItem}>
                        <input
                          type="hidden"
                          name="workPackageId"
                          value={workPackage.id}
                        />
                        <input type="hidden" name="scopeItemId" value={si.id} />
                        <button type="submit" className={styles.delete}>
                          {t("common.remove")}
                        </button>
                      </form>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </main>
  );
}
