"use client";

import { useState } from "react";
import Link from "next/link";
import {
  closestCorners,
  DndContext,
  DragOverlay,
  KeyboardSensor,
  PointerSensor,
  useDroppable,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from "@dnd-kit/core";
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import {
  duplicateLineItem,
  moveLineItem,
  removeLineItem,
  removeSubsectionLineItem,
} from "@/app/bills-of-quantities/actions";
import { BoqChevron } from "@/app/components/BoqChevron";
import { ConfirmDeleteButton } from "@/app/components/ConfirmDeleteButton";
import type { LineItem, Section } from "@/app/lib/api";
import { formatMoney, formatNumber } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import { useBoqAccordion } from "@/app/lib/useBoqAccordion";
import styles from "@/app/page.module.css";

// A line-item container the board can drag between: a section's directly-held lines (subsectionId
// null) or one subsection. Its `key` is the stable droppable id; the structure (which containers
// exist, their headings) is fixed during a drag — only line membership and order change.
interface Container {
  key: string;
  sectionId: string;
  subsectionId: string | null;
  label: string;
  isSection: boolean;
}

interface Board {
  layout: Container[];
  // Ordered line ids per container key.
  items: Record<string, string[]>;
  // Every line by id (content is read-only here; only position changes).
  lines: Record<string, LineItem>;
}

const sectionKey = (sectionId: string) => `sec:${sectionId}`;
const subKey = (sectionId: string, subsectionId: string) =>
  `sub:${sectionId}:${subsectionId}`;

// Flatten the BoQ's sections into the board model. Re-run to reconcile with the server's response
// (which carries the authoritative, renumbered order) after a successful move.
function buildBoard(sections: Section[]): Board {
  const layout: Container[] = [];
  const items: Record<string, string[]> = {};
  const lines: Record<string, LineItem> = {};

  for (const section of sections) {
    const sk = sectionKey(section.id);
    layout.push({
      key: sk,
      sectionId: section.id,
      subsectionId: null,
      label: `${section.sequence}. ${section.name}`,
      isSection: true,
    });
    items[sk] = section.lineItems.map((li) => li.id);
    for (const li of section.lineItems) lines[li.id] = li;

    for (const sub of section.subsections) {
      const k = subKey(section.id, sub.id);
      layout.push({
        key: k,
        sectionId: section.id,
        subsectionId: sub.id,
        label: `${section.sequence}.${sub.sequence} ${sub.name}`,
        isSection: false,
      });
      items[k] = sub.lineItems.map((li) => li.id);
      for (const li of sub.lineItems) lines[li.id] = li;
    }
  }

  return { layout, items, lines };
}

// A compact fingerprint of the sections' structure and per-line display data. Used to detect when
// a server revalidation (after a duplicate/remove/edit, which re-render this component with fresh
// props) has actually changed the bill, so the board can rebuild instead of showing stale rows.
function boardSignature(sections: Section[]): string {
  const lineSig = (li: LineItem) =>
    [li.id, li.sequence, li.description, li.quantity, li.unitOfMeasureId, li.lineTotalWithVat, li.notes];
  return JSON.stringify(
    sections.map((s) => [
      s.id,
      s.sequence,
      s.lineItems.map(lineSig),
      s.subsections.map((sub) => [sub.id, sub.sequence, sub.lineItems.map(lineSig)]),
    ]),
  );
}

/**
 * The Arrange-mode board: a single drag-and-drop surface spanning the whole BoQ so a line can be
 * reordered within its container or moved between a section's direct lines and any subsection
 * anywhere in the bill. Each drop calls the move action and reconciles to the server's renumbered
 * order; failures revert the optimistic change and surface a message. Read-first detail editing
 * (rename/remove/price) stays on the regular detail page.
 */
export function BoqDndBoard({
  bidId,
  boqId,
  sections,
  unitCode,
}: {
  // bidId builds the per-line edit hrefs (BoQ routes live under /bids/[id]/boq); boqId keys the
  // accordion state and the drag/mutation action fields.
  bidId: string;
  boqId: string;
  sections: Section[];
  unitCode: Record<string, string>;
}) {
  const [board, setBoard] = useState<Board>(() => buildBoard(sections));
  const [activeId, setActiveId] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Rebuild the board when the server sends genuinely different sections — i.e. after a row's
  // Edit/Duplicate/Delete revalidates the page (those actions re-render us with new props but the
  // board state was seeded only once). Comparing signatures (not references) avoids clobbering the
  // optimistic drag state, since a drop re-renders with the *old* props until its move resolves.
  const signature = boardSignature(sections);
  const [lastSignature, setLastSignature] = useState(signature);
  if (signature !== lastSignature) {
    setLastSignature(signature);
    setBoard(buildBoard(sections));
  }

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 4 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  // Collapse/expand state, shared with the read view and persisted per BoQ (same ids: section.id /
  // subsection.id). A collapsed container is simply not rendered — its drag droppable unmounts, so
  // it stops being a drop target until expanded again; board.items keeps all line data regardless.
  const allIds = sections.flatMap((s) => [s.id, ...s.subsections.map((ss) => ss.id)]);
  const { isOpen, toggle, setMany, allOpen, toggleAll } = useBoqAccordion(boqId, allIds);

  // Group the flat layout (section-direct lines, then each subsection) back into sections for the
  // accordion. Order is preserved: an isSection container opens a group; the non-section containers
  // that follow it are its subsections.
  const groups: { sectionId: string; section: Container; subs: Container[] }[] = [];
  for (const c of board.layout) {
    if (c.isSection) groups.push({ sectionId: c.sectionId, section: c, subs: [] });
    else groups[groups.length - 1]?.subs.push(c);
  }

  // Which container holds an id — or the container itself when `id` is a droppable container key
  // (e.g. dropping onto an empty subsection).
  function findContainer(id: string): string | null {
    if (id in board.items) return id;
    return board.layout.find((c) => board.items[c.key].includes(id))?.key ?? null;
  }

  function onDragStart(event: DragStartEvent) {
    setActiveId(String(event.active.id));
  }

  function onDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    setActiveId(null);
    if (!over) return;

    const lineId = String(active.id);
    const overId = String(over.id);
    const from = findContainer(lineId);
    const to = findContainer(overId);
    if (!from || !to) return;

    const snapshot = board.items;
    const items: Record<string, string[]> = {};
    for (const k of Object.keys(board.items)) items[k] = [...board.items[k]];

    let targetIndex: number;
    if (from === to) {
      const list = items[to];
      const oldIndex = list.indexOf(lineId);
      const overIndex = overId === to ? list.length - 1 : list.indexOf(overId);
      const newIndex = overIndex < 0 ? list.length - 1 : overIndex;
      if (oldIndex === newIndex) return; // dropped in place — nothing to do
      items[to] = arrayMove(list, oldIndex, newIndex);
      targetIndex = items[to].indexOf(lineId);
    } else {
      items[from] = items[from].filter((x) => x !== lineId);
      const toList = items[to];
      const overIndex = overId === to ? toList.length : toList.indexOf(overId);
      targetIndex = overIndex < 0 ? toList.length : overIndex;
      toList.splice(targetIndex, 0, lineId);
    }

    const meta = board.layout.find((c) => c.key === to);
    if (!meta) return;

    setBoard((prev) => ({ ...prev, items }));
    void persistMove(lineId, meta, targetIndex, snapshot);
  }

  async function persistMove(
    lineItemId: string,
    target: Container,
    targetIndex: number,
    snapshot: Record<string, string[]>,
  ) {
    setSaving(true);
    setError(null);
    const result = await moveLineItem({
      boqId,
      lineItemId,
      targetSectionId: target.sectionId,
      targetSubsectionId: target.subsectionId,
      targetIndex,
    });
    setSaving(false);

    if (!result.ok) {
      setError(result.error || t("boq.arrangeError"));
      setBoard((prev) => ({ ...prev, items: snapshot })); // revert the optimistic move
      return;
    }

    // Reconcile to the server's canonical, renumbered order.
    setBoard(buildBoard(result.boq.sections));
  }

  const activeLine = activeId ? board.lines[activeId] : null;

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCorners}
      onDragStart={onDragStart}
      onDragEnd={onDragEnd}
    >
      <p className={styles.muted}>
        {t("boq.arrangeHint")}
        {saving ? <> · {t("boq.arrangeSaving")}</> : null}
      </p>
      {error ? (
        <p className={styles.error} role="alert">
          {error}
        </p>
      ) : null}

      {allIds.length > 0 ? (
        <div className={styles.boqAccordionBar}>
          <button type="button" className={styles.edit} onClick={toggleAll}>
            {allOpen ? t("boq.collapseAll") : t("boq.expandAll")}
          </button>
        </div>
      ) : null}

      {groups.map((group) => {
        const sectionOpen = isOpen(group.sectionId);
        const subIds = group.subs
          .map((c) => c.subsectionId)
          .filter((id): id is string => id !== null);
        const allSubsOpen = subIds.length > 0 && subIds.every(isOpen);
        const sectionPanelId = `arrange-section-${group.sectionId}`;
        return (
          <div key={group.section.key}>
            <h2 style={{ marginTop: 24, display: "flex", alignItems: "center" }}>
              <button
                type="button"
                className={styles.boqDisclosure}
                aria-expanded={sectionOpen}
                aria-controls={sectionPanelId}
                onClick={() => toggle(group.sectionId)}
              >
                <BoqChevron open={sectionOpen} />
                <span>{group.section.label}</span>
              </button>
              {subIds.length > 0 ? (
                <button
                  type="button"
                  className={styles.boqToggleChildren}
                  onClick={() =>
                    allSubsOpen
                      ? setMany(subIds, false)
                      : setMany([group.sectionId, ...subIds], true)
                  }
                >
                  {allSubsOpen
                    ? t("boq.collapseSubsections")
                    : t("boq.expandSubsections")}
                </button>
              ) : null}
            </h2>

            <div id={sectionPanelId} hidden={!sectionOpen}>
              <ContainerList
                bidId={bidId}
                boqId={boqId}
                container={group.section}
                lineIds={board.items[group.section.key]}
                lines={board.lines}
                unitCode={unitCode}
              />

              {group.subs.map((container) => {
                const subOpen = container.subsectionId
                  ? isOpen(container.subsectionId)
                  : true;
                const subPanelId = `arrange-subsection-${container.subsectionId}`;
                return (
                  <div key={container.key}>
                    <h3 style={{ marginTop: 16, display: "flex", alignItems: "center" }}>
                      <button
                        type="button"
                        className={styles.boqDisclosure}
                        aria-expanded={subOpen}
                        aria-controls={subPanelId}
                        onClick={() =>
                          container.subsectionId && toggle(container.subsectionId)
                        }
                      >
                        <BoqChevron open={subOpen} />
                        <span>{container.label}</span>
                      </button>
                    </h3>
                    <div id={subPanelId} hidden={!subOpen}>
                      <ContainerList
                        bidId={bidId}
                        boqId={boqId}
                        container={container}
                        lineIds={board.items[container.key]}
                        lines={board.lines}
                        unitCode={unitCode}
                      />
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        );
      })}

      <DragOverlay>
        {activeLine ? (
          <div className={styles.dragRow} style={{ boxShadow: "0 4px 12px rgba(0,0,0,0.2)" }}>
            <span aria-hidden>⠿</span>
            <strong>{activeLine.description}</strong>
          </div>
        ) : null}
      </DragOverlay>
    </DndContext>
  );
}

// One container's lines as a vertical sortable list. The whole body is a droppable (keyed by the
// container key) so a line can be dropped into it even when empty.
function ContainerList({
  bidId,
  boqId,
  container,
  lineIds,
  lines,
  unitCode,
}: {
  bidId: string;
  boqId: string;
  container: Container;
  lineIds: string[];
  lines: Record<string, LineItem>;
  unitCode: Record<string, string>;
}) {
  const { setNodeRef, isOver } = useDroppable({ id: container.key });

  return (
    <SortableContext items={lineIds} strategy={verticalListSortingStrategy}>
      <div
        ref={setNodeRef}
        className={styles.dropList}
        style={isOver ? { outline: "2px dashed var(--accent, #888)" } : undefined}
      >
        {lineIds.length === 0 ? (
          <p className={styles.muted} style={{ padding: "8px 4px" }}>
            {t("boq.arrangeEmpty")}
          </p>
        ) : (
          lineIds.map((id, index) => (
            <SortableLineRow
              key={id}
              bidId={bidId}
              boqId={boqId}
              container={container}
              line={lines[id]}
              position={index + 1}
              unitCode={unitCode}
            />
          ))
        )}
      </div>
    </SortableContext>
  );
}

// A single draggable line row, kept compact — just the columns that matter while arranging
// (position, description, unit, qty, total) plus the same Edit/Duplicate/Remove actions as the
// read view, so a line can be fixed without leaving Arrange mode. Only the grip is the drag handle
// (the row body and its action controls stay clickable); which routes/actions a row edits and
// removes through is derived from its container (section-direct vs subsection).
function SortableLineRow({
  bidId,
  boqId,
  container,
  line,
  position,
  unitCode,
}: {
  bidId: string;
  boqId: string;
  container: Container;
  line: LineItem;
  position: number;
  unitCode: Record<string, string>;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } =
    useSortable({ id: line.id });

  const editHrefBase = container.isSection
    ? `/bids/${bidId}/boq/sections/${container.sectionId}/line-items`
    : `/bids/${bidId}/boq/sections/${container.sectionId}/subsections/${container.subsectionId}/line-items`;
  const removeAction = container.isSection ? removeLineItem : removeSubsectionLineItem;
  const removeFields = {
    boqId,
    sectionId: container.sectionId,
    ...(container.subsectionId ? { subsectionId: container.subsectionId } : {}),
    lineItemId: line.id,
  };

  return (
    <div
      ref={setNodeRef}
      className={styles.dragRow}
      style={{
        transform: CSS.Transform.toString(transform),
        transition,
        opacity: isDragging ? 0.4 : 1,
      }}
    >
      <button
        type="button"
        className={styles.dragHandle}
        aria-label={t("boq.arrangeDragHandle")}
        {...attributes}
        {...listeners}
      >
        <span aria-hidden>⠿</span>
      </button>
      <span style={{ width: "2ch", textAlign: "right" }}>{position}</span>
      <span style={{ flex: 1, minWidth: 0 }}>
        <strong>{line.description}</strong>
        {line.notes ? <div className={styles.muted}>{line.notes}</div> : null}
      </span>
      <span className={styles.muted}>{unitCode[line.unitOfMeasureId] ?? "—"}</span>
      <span className={styles.muted}>{formatNumber(line.quantity)}</span>
      <span>{formatMoney(line.lineTotalWithVat)}</span>
      <span className={styles.actions}>
        <Link href={`${editHrefBase}/${line.id}/edit`} className={styles.edit}>
          {t("common.edit")}
        </Link>
        <form action={duplicateLineItem}>
          <input type="hidden" name="boqId" value={boqId} />
          <input type="hidden" name="lineItemId" value={line.id} />
          <button type="submit" className={styles.edit}>
            {t("common.duplicate")}
          </button>
        </form>
        <ConfirmDeleteButton
          action={removeAction}
          fields={removeFields}
          title={t("lineItems.removeTitle")}
          bodyTemplate={t("lineItems.removeBody")}
          name={line.description}
          triggerLabel={t("common.remove")}
          confirmLabel={t("common.remove")}
        />
      </span>
    </div>
  );
}
