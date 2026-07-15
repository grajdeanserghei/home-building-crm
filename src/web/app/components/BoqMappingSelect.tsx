"use client";

import { useRef, useState } from "react";
import Link from "next/link";
import { setValuationLink } from "@/app/projects/[id]/valuation/actions";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// The catalog items offered as mapping targets — one flat list reused by every select.
export interface BoqMappingItem {
  id: string;
  printedNumber: string;
  name: string;
}

interface BoqMappingSelectProps {
  projectId: string;
  catalogId: string;
  boqId: string;
  bidId: string; // for revalidating this BoQ page after a change
  sectionId: string;
  subsectionId?: string | null; // the target's parent subsection (section-level ⇒ absent)
  lineItemId?: string | null; // present at line level (finest granularity), absent above it
  // The project's active catalog items (empty ⇒ no catalog / nothing to map to).
  catalogItems: BoqMappingItem[];
  // The item currently linked to this target, or undefined when unmapped.
  linkedItemId?: string;
  // Covered by a coarser link (whole section/subsection): the select is shown read-only with a hint
  // so the user unlinks the coarser mapping first (the backend also rejects it with a 409).
  disabled?: boolean;
  // Compact rendering for an inline per-line cell: drop the "mapat la:" prefix.
  compact?: boolean;
}

/**
 * The inline "mapat la: […]" control under a BoQ section/subsection header. Choosing an item (or
 * "— nemapat —") submits a progressively-enhanced form bound to `setValuationLink`, which
 * upserts/clears the link for this (boqId, sectionId, subsectionId?) triple and revalidates.
 *
 * A client component only so the native `<select>` can auto-submit on change. It appears
 * regardless of BoQ status — the mapping is project-level metadata, decoupled from BoQ edit
 * gating. Options are never disabled: one item may map to many targets, and a single-value
 * select keeps a target pointed at one item.
 */
export function BoqMappingSelect(props: BoqMappingSelectProps) {
  const { projectId, catalogItems, linkedItemId } = props;

  // No catalog for this project yet — point at the hub to create one, mirroring
  // ScenarioSelectionSelect's no-candidates message.
  if (catalogItems.length === 0) {
    return (
      <span className={styles.muted}>
        {t("valuation.map.prefix")}{" "}
        <Link href={`/projects/${projectId}/valuation`} className={styles.nameLink}>
          {t("valuation.map.noCatalog")}
        </Link>
      </span>
    );
  }

  // Keyed by the server-derived value so a revalidation (or an external change to the mapping)
  // remounts the control with fresh optimistic state — the canonical value wins once it changes,
  // without syncing state inside an effect.
  return <MappingForm key={linkedItemId ?? ""} {...props} />;
}

/**
 * The controlled select + its form. The select is **controlled** (not `defaultValue`): under
 * React 19 a form bound to a server action auto-resets its fields once the action settles, which
 * for an uncontrolled `<select>` snapped the choice back to its mount-time value until a full
 * reload. Controlled optimistic state keeps the user's choice on screen immediately; the parent's
 * `key` reconciles it to the server value after `revalidatePath` re-renders the page.
 */
function MappingForm({
  projectId,
  catalogId,
  boqId,
  bidId,
  sectionId,
  subsectionId,
  lineItemId,
  catalogItems,
  linkedItemId,
  disabled,
  compact,
}: BoqMappingSelectProps) {
  const formRef = useRef<HTMLFormElement>(null);
  const [selected, setSelected] = useState(linkedItemId ?? "");

  return (
    <form action={setValuationLink} ref={formRef} className={styles.boqMapForm}>
      {compact ? null : (
        <span className={styles.muted}>{t("valuation.map.prefix")}</span>
      )}
      <input type="hidden" name="projectId" value={projectId} />
      <input type="hidden" name="catalogId" value={catalogId} />
      <input type="hidden" name="boqId" value={boqId} />
      <input type="hidden" name="bidId" value={bidId} />
      <input type="hidden" name="sectionId" value={sectionId} />
      <input type="hidden" name="subsectionId" value={subsectionId ?? ""} />
      <input type="hidden" name="lineItemId" value={lineItemId ?? ""} />
      {/* The item currently holding this target — the action unlinks it before linking the new
          choice (the domain rejects double-mapping), giving replace-on-change. */}
      <input type="hidden" name="currentItemId" value={linkedItemId ?? ""} />
      <select
        name="itemId"
        aria-label={t("valuation.map.aria")}
        value={selected}
        disabled={disabled}
        title={disabled ? t("valuation.map.coveredHint") : undefined}
        onChange={(e) => {
          setSelected(e.target.value);
          formRef.current?.requestSubmit();
        }}
      >
        <option value="">
          {disabled ? t("valuation.map.covered") : t("valuation.map.none")}
        </option>
        {catalogItems.map((it) => (
          <option key={it.id} value={it.id}>
            {it.printedNumber}. {it.name}
          </option>
        ))}
      </select>
    </form>
  );
}
