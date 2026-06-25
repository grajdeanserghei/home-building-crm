import Link from "next/link";
import { notFound } from "next/navigation";
import { BidForm } from "@/app/components/BidForm";
import { openBid } from "@/app/bids/actions";
import {
  getBids,
  getContractors,
  getWorkPackage,
  type Bid,
  type Contractor,
} from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// Opening a bid is a deliberate act on its own route. Only contractors without a bid on
// this package are offered (the backend rejects a duplicate pair with a 409); when none are
// left the page explains why instead of showing an unusable form.
export default async function NewBidPage({
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
  try {
    [bids, contractors] = await Promise.all([getBids(id), getContractors()]);
  } catch {
    bids = [];
    contractors = [];
  }

  const taken = new Set(bids.map((b) => b.contractorId));
  const available = contractors.filter((c) => !taken.has(c.id));

  return (
    <main className={styles.main}>
      <Link href={`/work-packages/${workPackage.id}`} className={styles.backLink}>
        {t("workPackages.backToWorkPackage")}
      </Link>
      <h1>{t("workPackages.newBidTitle")}</h1>
      <p className={styles.subtitle}>{t("workPackages.openBidSubtitle")}</p>

      <section className={styles.card}>
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
        <Link
          href={`/work-packages/${workPackage.id}`}
          className={styles.backLink}
        >
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
