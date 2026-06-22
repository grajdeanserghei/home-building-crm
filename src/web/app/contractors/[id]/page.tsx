import Link from "next/link";
import { notFound } from "next/navigation";
import { getContractor, type Address } from "@/app/lib/api";
import styles from "@/app/page.module.css";

function formatDate(value?: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString();
}

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

  return (
    <main className={styles.main}>
      <Link href="/contractors" className={styles.backLink}>
        ← All contractors
      </Link>
      <h1>{contractor.name}</h1>
      <p className={styles.subtitle}>Contractor details.</p>

      <section className={styles.card}>
        <dl className={styles.detailList}>
          <dt>Fiscal code / CUI</dt>
          <dd>{contractor.fiscalCode || "—"}</dd>

          <dt>Trade register no.</dt>
          <dd>{contractor.registrationNumber || "—"}</dd>

          <dt>Contact person</dt>
          <dd>{contact?.personName || "—"}</dd>

          <dt>Email</dt>
          <dd>
            {contact?.email ? (
              <a href={`mailto:${contact.email}`}>{contact.email}</a>
            ) : (
              "—"
            )}
          </dd>

          <dt>Phone</dt>
          <dd>
            {contact?.phone ? (
              <a href={`tel:${contact.phone}`}>{contact.phone}</a>
            ) : (
              "—"
            )}
          </dd>

          <dt>Address</dt>
          <dd>{address || "—"}</dd>

          <dt>Notes</dt>
          <dd>{contractor.notes || "—"}</dd>

          <dt>Created</dt>
          <dd>{formatDate(contractor.createdAt)}</dd>
        </dl>
      </section>

      <Link href={`/contractors/${contractor.id}/edit`} className={styles.edit}>
        Edit contractor
      </Link>
    </main>
  );
}
