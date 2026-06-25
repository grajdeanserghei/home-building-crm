import Link from "next/link";
import { ContractorForm } from "../components/ContractorForm";
import { ConfirmDeleteButton } from "../components/ConfirmDeleteButton";
import { getContractors, type Contractor } from "../lib/api";
import { formatDate } from "../lib/format";
import { t } from "../lib/i18n";
import styles from "../page.module.css";
import { deleteContractor, registerContractor } from "./actions";

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
    error = e instanceof Error ? e.message : t("common.unknownError");
  }

  return (
    <main className={styles.main}>
      <h1>{t("contractors.title")}</h1>
      <p className={styles.subtitle}>{t("contractors.subtitle")}</p>

      <section className={styles.card}>
        <h2>{t("contractors.new")}</h2>
        <ContractorForm
          action={registerContractor}
          submitLabel={t("contractors.add")}
        />
      </section>

      <section className={styles.card}>
        <h2>{t("contractors.all")}</h2>
        {error ? (
          <p className={styles.error}>{t("common.apiError", { error })}</p>
        ) : contractors.length === 0 ? (
          <p>{t("contractors.empty")}</p>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t("common.name")}</th>
                <th>{t("contractors.contact")}</th>
                <th>{t("contractors.fiscalCode")}</th>
                <th>{t("common.created")}</th>
                <th aria-label={t("common.actions")} />
              </tr>
            </thead>
            <tbody>
              {contractors.map((c) => (
                <tr key={c.id}>
                  <td>
                    <Link href={`/contractors/${c.id}`} className={styles.nameLink}>
                      <strong>{c.name}</strong>
                    </Link>
                    {c.reference ? (
                      <div className={styles.muted}>{c.reference}</div>
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
                        {t("common.edit")}
                      </Link>
                      <ConfirmDeleteButton
                        action={deleteContractor}
                        fields={{ id: c.id }}
                        title={t("contractors.deleteTitle")}
                        bodyTemplate={t("contractors.deleteBody")}
                        name={c.name}
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
        ← {t("contractors.backToProjects")}
      </Link>
    </main>
  );
}
