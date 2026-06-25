import Link from "next/link";
import { notFound } from "next/navigation";
import { BidNoteForm } from "@/app/components/BidNoteForm";
import { logBidNoteOnProject } from "@/app/bids/actions";
import {
  BID_STATUS_LABELS,
  getBids,
  getContractors,
  getProject,
  getWorkPackages,
  NOTE_TYPE_LABELS,
  WORK_PACKAGE_STATUS_LABELS,
  type Bid,
  type Contractor,
  type DiscussionNote,
  type WorkPackage,
} from "@/app/lib/api";
import { formatDate } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

function bidCountLabel(count: number): string {
  return count === 1
    ? t("projectBids.bidCountOne")
    : t("projectBids.bidCountMany", { count: String(count) });
}

// The contractor's actionable contact line: person, tappable phone (tel:) and email
// (mailto:) so the owner can call from the overview. Null when nothing is on file.
function ContactRow({ contractor }: { contractor?: Contractor }) {
  const contact = contractor?.contact;
  const person = contact?.personName?.trim();
  const phone = contact?.phone?.trim();
  const email = contact?.email?.trim();

  if (!person && !phone && !email) {
    return <p className={styles.muted}>{t("projectBids.noContact")}</p>;
  }

  return (
    <div className={styles.contactRow}>
      {person ? <span>{person}</span> : null}
      {phone ? <a href={`tel:${phone}`}>{phone}</a> : null}
      {email ? <a href={`mailto:${email}`}>{email}</a> : null}
    </div>
  );
}

// The most recent discussion note (the log is oldest-first, so the last entry), or a
// muted placeholder when the bid has none yet.
function LastNote({ notes }: { notes: DiscussionNote[] }) {
  const note = notes[notes.length - 1];
  if (!note) {
    return <p className={styles.muted}>{t("projectBids.noNotes")}</p>;
  }
  return (
    <p className={styles.lastNote}>
      <span className={styles.noteLabel}>{t("projectBids.lastNote")}</span>
      {" · "}
      {formatDate(note.occurredOn)} · {NOTE_TYPE_LABELS[note.type]}
      <br />
      {note.content}
    </p>
  );
}

export default async function ProjectBidsPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const project = await getProject(id);

  if (!project) {
    notFound();
  }

  let workPackages: WorkPackage[] = [];
  let contractors: Contractor[] = [];
  // One bid list per work package, index-aligned with `workPackages`.
  let bidsByPackage: Bid[][] = [];
  let error: string | null = null;

  try {
    [workPackages, contractors] = await Promise.all([
      getWorkPackages(id),
      getContractors(),
    ]);
    // Bids live under their work package; there is no project-wide endpoint yet, so
    // gather them per package (a handful of packages — fine to fan out here).
    bidsByPackage = await Promise.all(
      workPackages.map((wp) => getBids(wp.id)),
    );
  } catch (e) {
    error = e instanceof Error ? e.message : t("common.unknownError");
  }

  const contractorById = new Map(contractors.map((c) => [c.id, c]));
  // Server-side clock for the add-note picker seed; keeps the form a server component.
  const today = new Date().toISOString().slice(0, 10);

  return (
    <main className={styles.main}>
      <Link href={`/projects/${project.id}`} className={styles.backLink}>
        {t("projectBids.backToProject")}
      </Link>
      <h1>{t("projectBids.title", { name: project.name })}</h1>
      <p className={styles.subtitle}>{t("projectBids.subtitle")}</p>

      {error ? (
        <p className={styles.error}>{t("common.apiError", { error })}</p>
      ) : workPackages.length === 0 ? (
        <p>{t("projectBids.empty")}</p>
      ) : (
        workPackages.map((wp, i) => {
          const bids = bidsByPackage[i] ?? [];
          return (
            <section key={wp.id} className={styles.card}>
              <h2>
                <Link
                  href={`/work-packages/${wp.id}`}
                  className={styles.nameLink}
                >
                  {wp.name}
                </Link>{" "}
                <span
                  className={`${styles.badge} ${styles[`status${wp.status}`]}`}
                >
                  {WORK_PACKAGE_STATUS_LABELS[wp.status]}
                </span>
                <span className={styles.muted}> · {bidCountLabel(bids.length)}</span>
              </h2>
              {bids.length === 0 ? (
                <p className={styles.muted}>{t("projectBids.noBids")}</p>
              ) : (
                bids.map((b) => {
                  const contractor = contractorById.get(b.contractorId);
                  return (
                    <div key={b.id} className={styles.offer}>
                      <div className={styles.offerHead}>
                        <h3>
                          {contractor?.name ??
                            t("workPackages.unknownContractor")}
                        </h3>
                        <span
                          className={`${styles.badge} ${styles[`status${b.status}`]}`}
                        >
                          {BID_STATUS_LABELS[b.status]}
                        </span>
                        <Link href={`/bids/${b.id}`} className={styles.nameLink}>
                          {t("projectBids.viewBid")}
                        </Link>
                      </div>

                      <ContactRow contractor={contractor} />

                      {contractor?.reference ? (
                        <p className={styles.muted}>{contractor.reference}</p>
                      ) : null}

                      {b.summary ? (
                        <p className={styles.muted}>{b.summary}</p>
                      ) : null}

                      <LastNote notes={b.notes} />

                      <BidNoteForm
                        action={logBidNoteOnProject}
                        bidId={b.id}
                        projectId={project.id}
                        today={today}
                      />
                    </div>
                  );
                })
              )}
            </section>
          );
        })
      )}
    </main>
  );
}
