import Link from "next/link";
import { notFound } from "next/navigation";
import { TradeChips } from "@/app/components/TradeChips";
import { getContractor, getTrades, type Address, type Trade } from "@/app/lib/api";
import { formatDate } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";
import { addContractorTrade, removeContractorTrade } from "../actions";

// Join the populated parts of an address into a single human-readable line,
// returning null when nothing is recorded.
function formatAddress(address?: Address | null): string | null {
  if (!address) return null;
  const parts = [
    address.street,
    address.city,
    address.county,
    address.postalCode,
    address.country,
  ].filter((p): p is string => Boolean(p && p.trim()));
  return parts.length > 0 ? parts.join(", ") : null;
}

export default async function ContractorDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const contractor = await getContractor(id);

  if (!contractor) {
    notFound();
  }

  const contact = contractor.contact;
  const address = formatAddress(contractor.address);

  // Load the vocabulary to resolve the firm's trade ids to names (all trades, so a
  // retired-but-still-assigned trade still shows a label) and to offer the active,
  // not-yet-assigned ones in the add control. Tolerate an API hiccup with empty lists.
  let allTrades: Trade[] = [];
  try {
    allTrades = await getTrades();
  } catch {
    allTrades = [];
  }

  const tradeNameById = new Map(allTrades.map((tr) => [tr.id, tr.name]));
  const assignedTrades = contractor.tradeIds.map((tid) => ({
    id: tid,
    name: tradeNameById.get(tid) ?? tid,
  }));
  const assignedIds = new Set(contractor.tradeIds);
  const availableTrades = allTrades
    .filter((tr) => tr.isActive && !assignedIds.has(tr.id))
    .map((tr) => ({ id: tr.id, name: tr.name }));

  return (
    <main className={styles.main}>
      <Link href="/contractors" className={styles.backLink}>
        ← {t("contractors.backToAll")}
      </Link>
      <h1>{contractor.name}</h1>
      <p className={styles.subtitle}>{t("contractors.detailsSubtitle")}</p>

      <section className={styles.card}>
        <dl className={styles.detailList}>
          <dt>{t("contractors.fiscalCode")}</dt>
          <dd>{contractor.fiscalCode || "—"}</dd>

          <dt>{t("contractors.registrationNumber")}</dt>
          <dd>{contractor.registrationNumber || "—"}</dd>

          <dt>{t("contractors.contactPerson")}</dt>
          <dd>{contact?.personName || "—"}</dd>

          <dt>{t("contractors.email")}</dt>
          <dd>
            {contact?.email ? (
              <a href={`mailto:${contact.email}`}>{contact.email}</a>
            ) : (
              "—"
            )}
          </dd>

          <dt>{t("contractors.phone")}</dt>
          <dd>
            {contact?.phone ? (
              <a href={`tel:${contact.phone}`}>{contact.phone}</a>
            ) : (
              "—"
            )}
          </dd>

          <dt>{t("contractors.address")}</dt>
          <dd>{address || "—"}</dd>

          <dt>{t("common.notes")}</dt>
          <dd>{contractor.notes || "—"}</dd>

          <dt>{t("common.created")}</dt>
          <dd>{formatDate(contractor.createdAt)}</dd>
        </dl>
      </section>

      <section className={styles.card}>
        <h2>{t("contractors.trades")}</h2>
        <TradeChips
          assigned={assignedTrades}
          available={availableTrades}
          ownerFieldName="id"
          ownerId={contractor.id}
          addAction={addContractorTrade}
          removeAction={removeContractorTrade}
          emptyLabel={t("contractors.tradesEmpty")}
          addLabel={t("contractors.addTrade")}
          selectPlaceholder={t("contractors.selectTrade")}
          allAssignedLabel={t("contractors.allTradesAssigned")}
          removeAriaLabel={(name) => t("contractors.removeTradeAria", { name })}
        />
      </section>

      <Link href={`/contractors/${contractor.id}/edit`} className={styles.edit}>
        {t("contractors.editContractor")}
      </Link>
    </main>
  );
}
