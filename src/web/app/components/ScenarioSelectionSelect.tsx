"use client";

import { useRef, useState } from "react";
import { setScenarioSelection } from "@/app/cost-scenarios/actions";
import { displayMoney, type DisplayCurrency } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import type { ScenarioCandidateBid } from "@/app/lib/api";
import styles from "@/app/page.module.css";

interface ScenarioSelectionSelectProps {
  scenarioId: string;
  projectId: string;
  workPackageId: string;
  // The priced bids available for this work package.
  candidates: ScenarioCandidateBid[];
  // The currently chosen bid id, or "" when the work package is not included.
  selectedBidId: string;
  // The global display currency (the header toggle), and the app-wide rate used to convert.
  displayCurrency: DisplayCurrency;
  ronPerEur: number;
}

/**
 * Per-row bid picker on a cost scenario's detail page. Choosing a bid (or "not included")
 * submits a progressively-enhanced form bound to the `setScenarioSelection` server action,
 * which upserts/clears the selection and revalidates the page so the breakdown and totals
 * update in place. Rendered as a client component only so the native `<select>` can
 * auto-submit on change (mirrors ProjectSwitcher).
 */
export function ScenarioSelectionSelect(props: ScenarioSelectionSelectProps) {
  if (props.candidates.length === 0) {
    return (
      <span className={styles.muted}>{t("costScenario.noCandidates")}</span>
    );
  }

  // Keyed by the server-derived value so a revalidation (or an external change) remounts the
  // control with fresh optimistic state — the canonical value wins once it changes, without
  // syncing state inside an effect.
  return <SelectionForm key={props.selectedBidId} {...props} />;
}

/**
 * The controlled select + its form. The select is **controlled** (not `defaultValue`): under
 * React 19 a form bound to a server action auto-resets its fields once the action settles, which
 * for an uncontrolled `<select>` snapped the choice back to its mount-time value until a full
 * reload. Controlled optimistic state keeps the user's choice on screen immediately; the parent's
 * `key` reconciles it to the server value after `revalidatePath` re-renders the page.
 */
function SelectionForm({
  scenarioId,
  projectId,
  workPackageId,
  candidates,
  selectedBidId,
  displayCurrency,
  ronPerEur,
}: ScenarioSelectionSelectProps) {
  const formRef = useRef<HTMLFormElement>(null);
  const [selected, setSelected] = useState(selectedBidId);

  return (
    <form action={setScenarioSelection} ref={formRef}>
      <input type="hidden" name="scenarioId" value={scenarioId} />
      <input type="hidden" name="projectId" value={projectId} />
      <input type="hidden" name="workPackageId" value={workPackageId} />
      <select
        name="bidId"
        aria-label={t("costScenario.choosebid")}
        value={selected}
        onChange={(e) => {
          setSelected(e.target.value);
          formRef.current?.requestSubmit();
        }}
      >
        <option value="">{t("costScenario.notIncluded")}</option>
        {candidates.map((bid) => (
          <option key={bid.bidId} value={bid.bidId}>
            {bid.contractorName}
            {bid.label ? ` · ${bid.label}` : ""} —{" "}
            {displayMoney(bid.gross, displayCurrency, ronPerEur)}
          </option>
        ))}
      </select>
    </form>
  );
}
