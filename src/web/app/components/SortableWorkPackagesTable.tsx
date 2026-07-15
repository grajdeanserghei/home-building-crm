"use client";

import { useState } from "react";
import Link from "next/link";
import {
  DndContext,
  DragOverlay,
  KeyboardSensor,
  PointerSensor,
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
import { WORK_PACKAGE_STATUS_LABELS, type WorkPackage } from "@/app/lib/api";
import {
  deleteWorkPackage,
  reorderWorkPackages,
} from "@/app/work-packages/actions";
import { ConfirmDeleteButton } from "@/app/components/ConfirmDeleteButton";
import { formatDate } from "@/app/lib/format";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface SortableWorkPackagesTableProps {
  // The project's work packages, in display order (as returned by the API, ordered by sequence).
  workPackages: WorkPackage[];
  // Owning project id — needed by the reorder and delete actions to revalidate the project's list.
  projectId: string;
  // Error string from loading the work packages, if any.
  error?: string | null;
}

// A compact fingerprint of the list's order and per-row display data. Used to detect when a server
// revalidation (after a reorder/delete/edit re-renders us with fresh props) has actually changed the
// list, so the board rebuilds instead of showing stale rows — while a drop-in-progress re-render
// (still the old props) leaves the optimistic order untouched.
function signatureOf(workPackages: WorkPackage[]): string {
  return JSON.stringify(
    workPackages.map((wp) => [
      wp.id,
      wp.sequence,
      wp.name,
      wp.description,
      wp.status,
      wp.plannedStartDate,
      wp.plannedEndDate,
    ]),
  );
}

/**
 * The project's work-packages list as a drag-and-drop sortable surface: drag a row by its grip to
 * reorder the packages within the project. Each drop optimistically reorders, then calls the reorder
 * action and reconciles to the server's renumbered (1..n) order; a failure reverts and surfaces a
 * message. Detail editing (rename/schedule/delete) stays on the per-package routes, reachable from
 * each row's Edit/Delete controls. Used on the project overview only — the home dashboard keeps the
 * plain, non-draggable `WorkPackagesTable`.
 */
export function SortableWorkPackagesTable({
  workPackages,
  projectId,
  error,
}: SortableWorkPackagesTableProps) {
  const [items, setItems] = useState<WorkPackage[]>(workPackages);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  // Rebuild from props when the server sends a genuinely different list (an edit/delete/reorder
  // elsewhere revalidated the page). Comparing signatures — not references — avoids clobbering the
  // optimistic drag state, since a drop re-renders with the *old* props until its action resolves.
  const signature = signatureOf(workPackages);
  const [lastSignature, setLastSignature] = useState(signature);
  if (signature !== lastSignature) {
    setLastSignature(signature);
    setItems(workPackages);
  }

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 4 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  if (error) {
    return <p className={styles.error}>{t("common.apiError", { error })}</p>;
  }

  if (items.length === 0) {
    return <p>{t("workPackages.empty")}</p>;
  }

  function onDragStart(event: DragStartEvent) {
    setActiveId(String(event.active.id));
  }

  function onDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    setActiveId(null);
    if (!over || active.id === over.id) return;

    const oldIndex = items.findIndex((wp) => wp.id === active.id);
    const newIndex = items.findIndex((wp) => wp.id === over.id);
    if (oldIndex < 0 || newIndex < 0) return;

    const snapshot = items;
    const reordered = arrayMove(items, oldIndex, newIndex);
    setItems(reordered);
    void persist(reordered.map((wp) => wp.id), snapshot);
  }

  async function persist(orderedIds: string[], snapshot: WorkPackage[]) {
    setSaving(true);
    setSaveError(null);
    const result = await reorderWorkPackages({ projectId, orderedIds });
    setSaving(false);

    if (!result.ok) {
      setSaveError(result.error || t("workPackages.reorderError"));
      setItems(snapshot); // revert the optimistic reorder
      return;
    }

    // Reconcile to the server's canonical, renumbered order.
    setItems(result.workPackages);
  }

  const activeItem = activeId
    ? items.find((wp) => wp.id === activeId) ?? null
    : null;

  return (
    <DndContext
      sensors={sensors}
      onDragStart={onDragStart}
      onDragEnd={onDragEnd}
    >
      <p className={styles.muted}>
        {t("workPackages.reorderHint")}
        {saving ? <> · {t("workPackages.reorderSaving")}</> : null}
      </p>
      {saveError ? (
        <p className={styles.error} role="alert">
          {saveError}
        </p>
      ) : null}

      <SortableContext
        items={items.map((wp) => wp.id)}
        strategy={verticalListSortingStrategy}
      >
        <div className={styles.dropList}>
          {items.map((wp, index) => (
            <SortableWorkPackageRow
              key={wp.id}
              workPackage={wp}
              position={index + 1}
              projectId={projectId}
            />
          ))}
        </div>
      </SortableContext>

      <DragOverlay>
        {activeItem ? (
          <div
            className={styles.dragRow}
            style={{ boxShadow: "0 4px 12px rgba(0,0,0,0.2)" }}
          >
            <span aria-hidden>⠿</span>
            <strong>{activeItem.name}</strong>
          </div>
        ) : null}
      </DragOverlay>
    </DndContext>
  );
}

// A single draggable work-package row. Only the grip is the drag handle (the name link, status
// badge, and Edit/Delete controls stay clickable); the position number reflects the live order.
function SortableWorkPackageRow({
  workPackage,
  position,
  projectId,
}: {
  workPackage: WorkPackage;
  position: number;
  projectId: string;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } =
    useSortable({ id: workPackage.id });

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
        aria-label={t("workPackages.reorderDragHandle")}
        {...attributes}
        {...listeners}
      >
        <span aria-hidden>⠿</span>
      </button>
      <span style={{ width: "2ch", textAlign: "right" }}>{position}</span>
      <span style={{ flex: 1, minWidth: 0 }}>
        <Link href={`/work-packages/${workPackage.id}`} className={styles.nameLink}>
          <strong>{workPackage.name}</strong>
        </Link>
        {workPackage.description ? (
          <div className={styles.muted}>{workPackage.description}</div>
        ) : null}
      </span>
      <span className={`${styles.badge} ${styles[`status${workPackage.status}`]}`}>
        {WORK_PACKAGE_STATUS_LABELS[workPackage.status]}
      </span>
      <span className={styles.muted}>{formatDate(workPackage.plannedStartDate)}</span>
      <span className={styles.muted}>{formatDate(workPackage.plannedEndDate)}</span>
      <span className={styles.actions}>
        <Link href={`/work-packages/${workPackage.id}/edit`} className={styles.edit}>
          {t("common.edit")}
        </Link>
        <ConfirmDeleteButton
          action={deleteWorkPackage}
          fields={{ id: workPackage.id, projectId }}
          title={t("workPackages.deleteTitle")}
          bodyTemplate={t("workPackages.deleteBody")}
          name={workPackage.name}
        />
      </span>
    </div>
  );
}
