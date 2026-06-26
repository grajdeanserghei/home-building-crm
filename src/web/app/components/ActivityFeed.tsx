import Link from "next/link";
import { NOTE_TYPE_LABELS, type ActivityItem } from "@/app/lib/api";
import { formatDateTime } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// Maps a stakeholder's user id to a display name. Empty until real authentication lands — every
// action is attributed to the single stub user today — so resolution falls back to a generic
// label. Fill this in (id → name) once the four stakeholders have real user ids.
const STAKEHOLDER_NAMES: Record<string, string> = {};

function authorName(authorId?: string | null): string {
  if (authorId && STAKEHOLDER_NAMES[authorId]) return STAKEHOLDER_NAMES[authorId];
  return t("feed.teamMember");
}

// The headline sentence for one entry, composed per kind.
function headline(item: ActivityItem): string {
  switch (item.kind) {
    case "NoteLogged":
      return t("feed.noteLogged", { author: authorName(item.authorId) });
    case "BidOpened":
      return item.contractorName
        ? t("feed.bidOpened", { contractor: item.contractorName })
        : t("feed.bidOpenedNoName");
    case "WorkPackageAdded":
      return t("feed.workPackageAdded", { name: item.workPackageName ?? "" });
    case "ProjectCreated":
      return t("feed.projectCreated");
  }
}

// Where clicking the entry drills in: the bid, else the work package, else nowhere.
function href(item: ActivityItem): string | null {
  if (item.bidId) return `/bids/${item.bidId}`;
  if (item.workPackageId) return `/work-packages/${item.workPackageId}`;
  return null;
}

// The muted context line: note type + contractor + work package for a note; the work package for
// the other kinds; always ending with the formatted time.
function meta(item: ActivityItem): string {
  const parts: string[] = [];
  if (item.kind === "NoteLogged") {
    if (item.noteType) parts.push(NOTE_TYPE_LABELS[item.noteType]);
    if (item.contractorName) parts.push(item.contractorName);
  }
  if (item.workPackageName) parts.push(item.workPackageName);
  parts.push(formatDateTime(item.timestamp));
  return parts.join(" · ");
}

/**
 * The home dashboard's recent-activity feed: a Facebook-style chronological list of discussion
 * notes (comments) and structural updates (bid opened, work package added, project created) for the
 * current project. Items arrive newest-first from the backend; this only renders them.
 */
export function ActivityFeed({ items }: { items: ActivityItem[] }) {
  if (items.length === 0) {
    return <p className={styles.muted}>{t("feed.empty")}</p>;
  }

  return (
    <ul className={styles.feedList}>
      {items.map((item, i) => {
        const link = href(item);
        const title = headline(item);
        const key = `${item.kind}-${item.bidId ?? item.workPackageId ?? "project"}-${item.timestamp}-${i}`;
        return (
          <li key={key} className={styles.feedItem}>
            <span className={styles.feedDot} aria-hidden />
            <div className={styles.feedBody}>
              <p className={styles.feedTitle}>
                {link ? (
                  <Link href={link} className={styles.nameLink}>
                    {title}
                  </Link>
                ) : (
                  title
                )}
              </p>
              <p className={styles.feedMeta}>{meta(item)}</p>
              {item.content ? (
                <p className={styles.feedContent}>{item.content}</p>
              ) : null}
            </div>
          </li>
        );
      })}
    </ul>
  );
}
