import Link from "next/link";
import { ContractorForm } from "../components/ContractorForm";
import { DeleteContractorButton } from "../components/DeleteContractorButton";
import { getContractors, type Contractor } from "../lib/api";
import styles from "../page.module.css";
import { deleteContractor, registerContractor } from "./actions";

function formatDate(value?: string | null): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString();
}

// Pick the most useful contact detail to show in the list row, falling back through
// person → email → phone, then an em dash when nothing is recorded.
function primaryContact(contractor: Contractor): string {
  const c = contractor.contact;
  return c?.personName || c?.email || c?.phone || "—";
}

export default async function ContractorsPage() {
  let contractors: Contractor[] = [];
  let error: string | null = null;

  try {
    contractors = await getContractors();
  } catch (e) {
    error = e instanceof Error ? e.message : "Unknown error";
  }

  return (
    <main className={styles.main}>
      <h1>Contractors</h1>
      <p className={styles.subtitle}>
        Firms that bid on and carry out work packages.
      </p>

      <section className={styles.card}>
        <h2>New contractor</h2>
        <ContractorForm action={registerContractor} submitLabel="Add contractor" />
      </section>

      <section className={styles.card}>
        <h2>All contractors</h2>
        {error ? (
          <p className={styles.error}>Could not reach the API: {error}</p>
        ) : contractors.length === 0 ? (
          <p>No contractors yet. Add your first one above.</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>Name</th>
                <th>Contact</th>
                <th>Fiscal code</th>
                <th>Created</th>
                <th aria-label="actions" />
              </tr>
            </thead>
            <tbody>
              {contractors.map((c) => (
                <tr key={c.id}>
                  <td>
                    <Link href={`/contractors/${c.id}`} className={styles.nameLink}>
                      <strong>{c.name}</strong>
                    </Link>
                    {c.notes ? (
                      <div className={styles.muted}>{c.notes}</div>
                    ) : null}
                  </td>
                  <td>{primaryContact(c)}</td>
                  <td>{c.fiscalCode || "—"}</td>
                  <td>{formatDate(c.createdAt)}</td>
                  <td>
                    <div className={styles.actions}>
                      <Link
                        href={`/contractors/${c.id}/edit`}
                        className={styles.edit}
                      >
                        Edit
                      </Link>
                      <DeleteContractorButton
                        action={deleteContractor}
                        contractorId={c.id}
                        contractorName={c.name}
                      />
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <Link href="/" className={styles.backLink}>
        ← Projects
      </Link>
    </main>
  );
}
