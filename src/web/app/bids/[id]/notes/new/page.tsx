import Link from "next/link";
import { notFound } from "next/navigation";
import { BidNoteForm } from "@/app/components/BidNoteForm";
import { logBidNote } from "@/app/bids/actions";
import { getBid } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

export default async function LogBidNotePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const bid = await getBid(id);

  if (!bid) {
    notFound();
  }

  // `new Date()` here runs server-side at request time; the picker seed is just a default.
  const today = new Date().toISOString().slice(0, 10);

  return (
    <main className={styles.main}>
      <Link href={`/bids/${bid.id}`} className={styles.backLink}>
        {t("bids.backToBid")}
      </Link>
      <h1>{t("notes.logHeading")}</h1>
      <p className={styles.subtitle}>{t("notes.logSubtitle")}</p>

      <section className={styles.card}>
        <BidNoteForm action={logBidNote} bidId={bid.id} today={today} />
      </section>
    </main>
  );
}
