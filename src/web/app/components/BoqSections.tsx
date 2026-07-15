"use client";

import { useMemo } from "react";
import Link from "next/link";
import {
  duplicateLineItem,
  removeLineItem,
  removeSection,
  removeSubsection,
  removeSubsectionLineItem,
} from "@/app/bills-of-quantities/actions";
import { BoqChevron } from "@/app/components/BoqChevron";
import { BoqMappingSelect, type BoqMappingItem } from "@/app/components/BoqMappingSelect";
import { ConfirmDeleteButton } from "@/app/components/ConfirmDeleteButton";
import { LineItemsTable } from "@/app/components/LineItemsTable";
import type { Money, Section } from "@/app/lib/api";
import { displayMoney, type DisplayCurrency } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import { useBoqAccordion } from "@/app/lib/useBoqAccordion";
import styles from "@/app/page.module.css";

/**
 * The read view of a BoQ's priced structure as a collapse/expand accordion: each Section and each
 * Subsection is a disclosure, with a single smart "expand/collapse all" toggle at the BoQ level and
 * an equivalent one per Section (for its subsections). Open/closed state is shared with Arrange mode
 * and persisted per BoQ (see useBoqAccordion). Renders the same section/line markup and edit actions
 * as before — only the headings became disclosure buttons and the bodies became collapsible.
 */
export function BoqSections({
  bidId,
  boqId,
  sections,
  unitCode,
  editable,
  displayCurrency,
  ronPerEur,
  projectId,
  catalogId,
  catalogItems,
  mappedItemBySection,
}: {
  // The owning bid's id builds the edit/add hrefs (the BoQ routes live under /bids/[id]/boq);
  // boqId still keys the accordion state and the mutation action fields.
  bidId: string;
  boqId: string;
  sections: Section[];
  unitCode: Record<string, string>;
  editable: boolean;
  // The global display currency (the header toggle) and the BoQ's "1 EUR = N RON" rate to convert
  // with. "Original" shows the pricing currency with decimals; RON/EUR convert and drop decimals.
  displayCurrency: DisplayCurrency;
  ronPerEur: number;
  // The owning project + its valuation catalog, for the per-header mapping control. `catalogId`
  // is "" when the project has no catalog yet; `catalogItems` (active items) is then empty, and
  // each header shows a "Fără fișă de evaluare" hint instead of a select.
  projectId: string;
  catalogId: string;
  catalogItems: BoqMappingItem[];
  // The catalog item currently linked to a section/subsection, keyed by that section's or
  // subsection's id (a triple maps to at most one item, so a scalar per key suffices).
  mappedItemBySection: Record<string, string>;
}) {
  // Format a Money for the chosen display currency (see displayMoney: decimals only in Original mode).
  const money = (m: Money) => displayMoney(m, displayCurrency, ronPerEur);

  // LineItemsTable expects a Map; rebuild it once from the plain object passed across the boundary.
  const unitCodeMap = useMemo(() => new Map(Object.entries(unitCode)), [unitCode]);
  const allIds = useMemo(
    () => sections.flatMap((s) => [s.id, ...s.subsections.map((ss) => ss.id)]),
    [sections],
  );
  const { isOpen, toggle, setMany, allOpen, toggleAll } = useBoqAccordion(boqId, allIds);

  return (
    <>
      <div className={styles.boqAccordionBar}>
        <button type="button" className={styles.edit} onClick={toggleAll}>
          {allOpen ? t("boq.collapseAll") : t("boq.expandAll")}
        </button>
      </div>

      {sections.map((section) => {
        const sectionOpen = isOpen(section.id);
        const subIds = section.subsections.map((sub) => sub.id);
        const allSubsOpen = subIds.length > 0 && subIds.every(isOpen);
        const sectionPanelId = `boq-section-${section.id}`;
        return (
          <section className={`${styles.card} ${styles.boqSection}`} key={section.id}>
            <h2>
              <button
                type="button"
                className={styles.boqDisclosure}
                aria-expanded={sectionOpen}
                aria-controls={sectionPanelId}
                onClick={() => toggle(section.id)}
              >
                <BoqChevron open={sectionOpen} />
                <span>
                  {section.sequence}. {section.name}{" "}
                  <span className={styles.muted}>
                    · {money(section.subtotalWithVat)} {t("boq.inclVat")} (
                    {money(section.subtotal)} {t("boq.exclShort")})
                  </span>
                </span>
              </button>
              {subIds.length > 0 ? (
                <button
                  type="button"
                  className={styles.boqToggleChildren}
                  onClick={() =>
                    allSubsOpen
                      ? setMany(subIds, false)
                      : setMany([section.id, ...subIds], true)
                  }
                >
                  {allSubsOpen
                    ? t("boq.collapseSubsections")
                    : t("boq.expandSubsections")}
                </button>
              ) : null}
            </h2>

            <BoqMappingSelect
              projectId={projectId}
              catalogId={catalogId}
              boqId={boqId}
              bidId={bidId}
              sectionId={section.id}
              catalogItems={catalogItems}
              linkedItemId={mappedItemBySection[section.id]}
            />

            <div id={sectionPanelId} hidden={!sectionOpen}>
              {section.description ? (
                <p className={styles.muted}>{section.description}</p>
              ) : null}

              <LineItemsTable
                lineItems={section.lineItems}
                unitCode={unitCodeMap}
                editable={editable}
                boqId={boqId}
                sectionId={section.id}
                editHrefBase={`/bids/${bidId}/boq/sections/${section.id}/line-items`}
                removeAction={removeLineItem}
                duplicateAction={duplicateLineItem}
                displayCurrency={displayCurrency}
                ronPerEur={ronPerEur}
              />

              {/* Subsections: an optional second level of grouping within the section. */}
              {section.subsections.map((subsection) => {
                const subOpen = isOpen(subsection.id);
                const subPanelId = `boq-subsection-${subsection.id}`;
                return (
                  <div className={styles.subsection} key={subsection.id}>
                    <h3>
                      <button
                        type="button"
                        className={styles.boqDisclosure}
                        aria-expanded={subOpen}
                        aria-controls={subPanelId}
                        onClick={() => toggle(subsection.id)}
                      >
                        <BoqChevron open={subOpen} />
                        <span>
                          {section.sequence}.{subsection.sequence} {subsection.name}{" "}
                          <span className={styles.muted}>
                            · {money(subsection.subtotalWithVat)} {t("boq.inclVat")} (
                            {money(subsection.subtotal)} {t("boq.exclShort")})
                          </span>
                        </span>
                      </button>
                    </h3>

                    <BoqMappingSelect
                      projectId={projectId}
                      catalogId={catalogId}
                      boqId={boqId}
                      bidId={bidId}
                      sectionId={section.id}
                      subsectionId={subsection.id}
                      catalogItems={catalogItems}
                      linkedItemId={mappedItemBySection[subsection.id]}
                    />

                    <div id={subPanelId} hidden={!subOpen}>
                      {subsection.description ? (
                        <p className={styles.muted}>{subsection.description}</p>
                      ) : null}

                      <LineItemsTable
                        lineItems={subsection.lineItems}
                        unitCode={unitCodeMap}
                        editable={editable}
                        boqId={boqId}
                        sectionId={section.id}
                        subsectionId={subsection.id}
                        editHrefBase={`/bids/${bidId}/boq/sections/${section.id}/subsections/${subsection.id}/line-items`}
                        removeAction={removeSubsectionLineItem}
                        duplicateAction={duplicateLineItem}
                        displayCurrency={displayCurrency}
                        ronPerEur={ronPerEur}
                      />

                      {editable ? (
                        <div className={styles.actions}>
                          <Link
                            href={`/bids/${bidId}/boq/sections/${section.id}/subsections/${subsection.id}/line-items/new`}
                            className={styles.edit}
                          >
                            {t("subsections.addLine")}
                          </Link>
                          <Link
                            href={`/bids/${bidId}/boq/sections/${section.id}/subsections/${subsection.id}/edit`}
                            className={styles.edit}
                          >
                            {t("subsections.edit")}
                          </Link>
                          <ConfirmDeleteButton
                            action={removeSubsection}
                            fields={{
                              boqId,
                              sectionId: section.id,
                              subsectionId: subsection.id,
                            }}
                            title={t("subsections.removeTitle")}
                            bodyTemplate={t("subsections.removeBody")}
                            name={subsection.name}
                            triggerLabel={t("subsections.remove")}
                            confirmLabel={t("subsections.remove")}
                          />
                        </div>
                      ) : null}
                    </div>
                  </div>
                );
              })}

              {editable ? (
                <div className={styles.actions} style={{ marginTop: 16 }}>
                  <Link
                    href={`/bids/${bidId}/boq/sections/${section.id}/line-items/new`}
                    className={styles.edit}
                  >
                    {t("lineItems.add")}
                  </Link>
                  <Link
                    href={`/bids/${bidId}/boq/sections/${section.id}/subsections/new`}
                    className={styles.edit}
                  >
                    {t("subsections.add")}
                  </Link>
                  <Link
                    href={`/bids/${bidId}/boq/sections/${section.id}/edit`}
                    className={styles.edit}
                  >
                    {t("sections.edit")}
                  </Link>
                  <ConfirmDeleteButton
                    action={removeSection}
                    fields={{ boqId, sectionId: section.id }}
                    title={t("sections.removeTitle")}
                    bodyTemplate={t("sections.removeBody")}
                    name={section.name}
                    triggerLabel={t("sections.remove")}
                    confirmLabel={t("sections.remove")}
                  />
                </div>
              ) : null}
            </div>
          </section>
        );
      })}
    </>
  );
}
