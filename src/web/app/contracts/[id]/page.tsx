import Link from "next/link";
import { notFound } from "next/navigation";
import {
  changeContractStatus,
  deleteContract,
} from "@/app/contracts/actions";
import { ConfirmDeleteButton } from "@/app/components/ConfirmDeleteButton";
import {
  CONTRACT_STATUS_LABELS,
  CONTRACT_STATUSES,
  getBid,
  getBillOfQuantities,
  getContract,
  getContractor,
  getWorkPackage,
  type ContractStatus,
} from "@/app/lib/api";
import { formatDate, formatMoney } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// The statuses a contract may move to from its current one. Completed and Terminated
// are terminal (closed) — the backend forbids transitioning out of them.
function allowedTargets(current: ContractStatus): ContractStatus[] {
  if (current === "Completed" || current === "Terminated") return [];
  return CONTRACT_STATUSES.filter((s) => s !== current);
}

export default async function ContractDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const contract = await getContract(id);

  if (!contract) {
    notFound();
  }

  // Resolve the related entities for display: the awarded work package, the accepted
  // BoQ this contract is based on, and (through the BoQ's bid) the contractor.
  const [workPackage, boq] = await Promise.all([
    getWorkPackage(contract.workPackageId),
    getBillOfQuantities(contract.acceptedBoqId),
  ]);
  const bid = boq ? await getBid(boq.bidId) : null;
  const contractor = bid ? await getContractor(bid.contractorId) : null;

  const targets = allowedTargets(contract.status);
  const title = contract.contractNumber
    ? t("contracts.titleNumbered", { number: contract.contractNumber })
    : t("contracts.titleFor", {
        name: workPackage?.name ?? t("contracts.workPackageLower"),
      });

  return (
    <main className={styles.main}>
      <Link
        href={`/work-packages/${contract.workPackageId}`}
        className={styles.backLink}
      >
        {t("contracts.backTo", {
          name: workPackage?.name ?? t("contracts.workPackageLower"),
        })}
      </Link>
      <h1>{title}</h1>
      <p className={styles.subtitle}>
        {workPackage
          ? t("contracts.awardedFor", { name: workPackage.name })
          : t("contracts.awardedContract")}
        {contractor ? ` · ${contractor.name}` : ""}
        {" · "}
        <span className={`${styles.badge} ${styles[`status${contract.status}`]}`}>
          {CONTRACT_STATUS_LABELS[contract.status]}
        </span>
        {" · "}
        <strong>{formatMoney(contract.value)}</strong>
      </p>

      <section className={styles.card}>
        <dl className={styles.detailList}>
          <dt>{t("contracts.workPackage")}</dt>
          <dd>
            <Link
              href={`/work-packages/${contract.workPackageId}`}
              className={styles.nameLink}
            >
              {workPackage?.name ?? contract.workPackageId}
            </Link>
          </dd>
          <dt>{t("contracts.contractor")}</dt>
          <dd>
            {contractor ? (
              <>
                <Link
                  href={`/contractors/${contractor.id}`}
                  className={styles.nameLink}
                >
                  {contractor.name}
                </Link>
                {contractor.reference ? (
                  <div className={styles.muted}>{contractor.reference}</div>
                ) : null}
              </>
            ) : (
              "—"
            )}
          </dd>
          <dt>{t("contracts.acceptedBoq")}</dt>
          <dd>
            {boq ? (
              <Link
                href={`/bills-of-quantities/${boq.id}`}
                className={styles.nameLink}
              >
                {t("boq.title")}
                {boq.reference ? ` · ${boq.reference}` : ""} (
                {t("contracts.inclVat", {
                  amount: formatMoney(boq.totalWithVat),
                })}
                )
              </Link>
            ) : (
              "—"
            )}
          </dd>
          <dt>{t("contracts.contractNumber")}</dt>
          <dd>{contract.contractNumber || "—"}</dd>
          <dt>{t("common.status")}</dt>
          <dd>{CONTRACT_STATUS_LABELS[contract.status]}</dd>
          <dt>{t("contracts.agreedValue")}</dt>
          <dd>{formatMoney(contract.value)}</dd>
          <dt>{t("contracts.signedOn")}</dt>
          <dd>{formatDate(contract.signedOn)}</dd>
          <dt>{t("contracts.startDate")}</dt>
          <dd>{formatDate(contract.startDate)}</dd>
          <dt>{t("contracts.plannedEndDate")}</dt>
          <dd>{formatDate(contract.plannedEndDate)}</dd>
          <dt>{t("contracts.actualEndDate")}</dt>
          <dd>{formatDate(contract.actualEndDate)}</dd>
          <dt>{t("common.notes")}</dt>
          <dd>{contract.notes || "—"}</dd>
          <dt>{t("contracts.awarded")}</dt>
          <dd>{formatDate(contract.createdAt)}</dd>
        </dl>
        <div className={styles.actions}>
          <Link href={`/contracts/${contract.id}/edit`} className={styles.edit}>
            {t("common.edit")}
          </Link>
          <ConfirmDeleteButton
            action={deleteContract}
            fields={{ id: contract.id }}
            title={t("contracts.deleteTitle")}
            bodyTemplate={t("contracts.deleteBody")}
            name={title}
          />
        </div>
      </section>

      <section className={styles.card}>
        <h2>{t("contracts.changeStatus")}</h2>
        {targets.length === 0 ? (
          <p className={styles.muted}>
            {t("contracts.statusFinal", {
              status: CONTRACT_STATUS_LABELS[contract.status].toLowerCase(),
            })}
          </p>
        ) : (
          <form action={changeContractStatus} className={styles.form}>
            <input type="hidden" name="id" value={contract.id} />
            <label className={styles.fieldLabel}>
              {t("contracts.newStatus")}
              <select name="status" defaultValue={targets[0]}>
                {targets.map((s) => (
                  <option key={s} value={s}>
                    {CONTRACT_STATUS_LABELS[s]}
                  </option>
                ))}
              </select>
            </label>
            <span />
            <label className={styles.fieldLabel}>
              {t("contracts.signedOnHint")}
              <input name="signedOn" type="date" />
            </label>
            <label className={styles.fieldLabel}>
              {t("contracts.actualEndDateHint")}
              <input name="actualEndDate" type="date" />
            </label>
            <button type="submit">{t("contracts.updateStatus")}</button>
          </form>
        )}
        <p className={styles.muted}>{t("contracts.statusHelp")}</p>
      </section>
    </main>
  );
}
