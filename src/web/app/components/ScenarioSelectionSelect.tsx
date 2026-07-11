"use client";

import { useRef } from "react";
import { setScenarioSelection } from "@/app/cost-scenarios/actions";
import { formatMoneyIn } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import type { Currency, ScenarioCandidateBid } from "@/app/lib/api";
import styles from "@/app/page.module.css";

interface ScenarioSelectionSelectProps {
  scenarioId: string;
  projectId: string;
  workPackageId: string;
  // The priced bids available for this work package.
  candidates: ScenarioCandidateBid[];
  // The currently chosen bid id, or "" when the work package is not included.
  selectedBidId: string;
  // The currency to display each bid's price in, and the app-wide rate used to convert.
  displayCurrency: Currency;
  ronPerEur: number;
}

/**
 * Per-row bid picker on a cost scenario's detail page. Choosing a bid (or "not included")
 * submits a progressively-enhanced form bound to the `setScenarioSelection` server action,
 * which upserts/clears the selection and revalidates the page so the breakdown and totals
 * update in place. Rendered as a client component only so the native `<select>` can
 * auto-submit on change (mirrors ProjectSwitcher).
 */
export function ScenarioSelectionSelect({
  scenarioId,
  projectId,
  workPackageId,
  candidates,
  selectedBidId,
  displayCurrency,
  ronPerEur,
}: ScenarioSelectionSelectProps) {
  const formRef = useRef<HTMLFormElement>(null);

  if (candidates.length === 0) {
    return (
      <span className={styles.muted}>{t("costScenario.noCandidates")}</span>
    );
  }

  return (
    <form action={setScenarioSelection} ref={formRef}>
      <input type="hidden" name="scenarioId" value={scenarioId} />
      <input type="hidden" name="projectId" value={projectId} />
      <input type="hidden" name="workPackageId" value={workPackageId} />
      <select
        name="bidId"
        aria-label={t("costScenario.choosebid")}
        defaultValue={selectedBidId}
        onChange={() => formRef.current?.requestSubmit()}
      >
        <option value="">{t("costScenario.notIncluded")}</option>
        {candidates.map((bid) => (
          <option key={bid.bidId} value={bid.bidId}>
            {bid.contractorName} — {formatMoneyIn(bid.gross, displayCurrency, ronPerEur)}
          </option>
        ))}
      </select>
    </form>
  );
}
