import Link from "next/link";
import { notFound } from "next/navigation";
import { BidForm } from "@/app/components/BidForm";
import { ScopeItemForm } from "@/app/components/ScopeItemForm";
import { openBid } from "@/app/bids/actions";
import {
  addScopeItem,
  changeWorkPackageStatus,
  removeScopeItem,
} from "@/app/work-packages/actions";
import {
  BID_STATUS_LABELS,
  getBids,
  getContractors,
  getWorkPackage,
  SCOPE_ITEM_REQUIREMENT_LABELS,
  WORK_PACKAGE_STATUS_LABELS,
  type Bid,
  type Contractor,
  type WorkPackageStatus,
} from "@/app/lib/api";
import { formatDate } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// The statuses a package may move to from its current one, given the lifecycle. Awarded is
// excluded — it is reached only through the award flow (selecting a bid, creating a
// contract), not this control. Completed and Cancelled are terminal.
function allowedTargets(current: WorkPackageStatus): WorkPackageStatus[] {
  switch (current) {
    case "Defined":
      return ["OpenForBids", "Cancelled"];
    case "OpenForBids":
      return ["Defined", "Cancelled"];
    case "Awarded":
      return ["InProgress", "Cancelled"];
    case "InProgress":
      return ["Completed", "Cancelled"];
    default:
      return [];
  }
}

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
  let error: string | null = null;

  try {
    [bids, contractors] = await Promise.all([
      getBids(id),
      getContractors(),
    ]);
  } catch (e) {
    error = e instanceof Error ? e.message : t("common.unknownError");
  }

  // Map contractor id → name for the bids table, and offer only contractors that don't
  // already have a bid here (the backend rejects a duplicate pair with a 409).
  const contractorName = new Map(contractors.map((c) => [c.id, c.name]));
  const taken = new Set(bids.map((b) => b.contractorId));
  const available = contractors.filter((c) => !taken.has(c.id));

  const targets = allowedTargets(workPackage.status);
  const scopeItems = workPackage.scopeItems;

  return (
    <main className={styles.main}>
      <Link href={`/projects/${workPackage.projectId}`} className={styles.backLink}>
        {t("workPackages.backToProject")}
      </Link>
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
        <h2>{t("workPackages.changeStatusTitle")}</h2>
        {targets.length === 0 ? (
          <p className={styles.muted}>
            {t("workPackages.statusFinal", {
              status: WORK_PACKAGE_STATUS_LABELS[workPackage.status].toLowerCase(),
            })}
          </p>
        ) : (
          <form action={changeWorkPackageStatus} className={styles.form}>
            <input type="hidden" name="id" value={workPackage.id} />
            <input type="hidden" name="projectId" value={workPackage.projectId} />
            <select name="status" defaultValue={targets[0]}>
              {targets.map((s) => (
                <option key={s} value={s}>
                  {WORK_PACKAGE_STATUS_LABELS[s]}
                </option>
              ))}
            </select>
            <button type="submit">{t("workPackages.updateStatus")}</button>
          </form>
        )}
        <p className={styles.muted}>{t("workPackages.awardingHint")}</p>
      </section>

      <section className={styles.card}>
        <h2>{t("scopeItems.title")}</h2>
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

        <h2 style={{ marginTop: 20 }}>{t("scopeItems.add")}</h2>
        <ScopeItemForm
          action={addScopeItem}
          workPackageId={workPackage.id}
          defaultSequence={scopeItems.length + 1}
        />
      </section>

      <section className={styles.card}>
        <h2>{t("workPackages.newBidTitle")}</h2>
        {available.length === 0 ? (
          <p className={styles.muted}>
            {contractors.length === 0
              ? t("workPackages.noContractors")
              : t("workPackages.allContractorsBid")}
          </p>
        ) : (
          <BidForm
            action={openBid}
            workPackageId={workPackage.id}
            contractors={available}
            submitLabel={t("workPackages.openBid")}
          />
        )}
      </section>

      <section className={styles.card}>
        <h2>{t("workPackages.bidsTitle")}</h2>
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
    </main>
  );
}
