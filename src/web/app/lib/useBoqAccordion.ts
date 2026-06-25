import { useMemo, useSyncExternalStore } from "react";

// Shared collapse/expand state for a BoQ's Section/Subsection accordion, used by both the read view
// (BoqSections) and Arrange mode (BoqDndBoard). The open set holds the ids of expanded Sections AND
// Subsections — both are distinct GUIDs, so one Set covers them — and is persisted per BoQ in
// localStorage so a refresh, an inline edit (which revalidates the page) or switching between the
// read and Arrange views keeps the same items open. Default: all collapsed.
//
// localStorage is the single source of truth, read through useSyncExternalStore: the server snapshot
// is the empty (all-collapsed) default, so there's no hydration mismatch, and React safely re-reads
// the stored selection on the client after hydration. Writes notify same-tab subscribers via a
// custom event (the native `storage` event only fires in *other* tabs) and also sync across tabs.

const CHANGE_EVENT = "boq-accordion-change";

function readRaw(key: string): string {
  try {
    return localStorage.getItem(key) ?? "";
  } catch {
    return "";
  }
}

function writeRaw(key: string, value: string) {
  try {
    localStorage.setItem(key, value);
  } catch {
    // ignore unavailable storage
  }
  window.dispatchEvent(new CustomEvent(CHANGE_EVENT));
}

function subscribe(callback: () => void) {
  window.addEventListener(CHANGE_EVENT, callback);
  window.addEventListener("storage", callback);
  return () => {
    window.removeEventListener(CHANGE_EVENT, callback);
    window.removeEventListener("storage", callback);
  };
}

export function useBoqAccordion(boqId: string, allIds: string[]) {
  const key = `boq-accordion:${boqId}`;

  // A string snapshot (stable by value, so no re-render loop); "" on the server / before any write.
  const raw = useSyncExternalStore(
    subscribe,
    () => readRaw(key),
    () => "",
  );

  const open = useMemo<Set<string>>(() => {
    if (!raw) return new Set();
    try {
      return new Set(JSON.parse(raw) as string[]);
    } catch {
      return new Set();
    }
  }, [raw]);

  const persist = (next: Set<string>) => writeRaw(key, JSON.stringify([...next]));

  const isOpen = (id: string) => open.has(id);

  const toggle = (id: string) => {
    const next = new Set(open);
    if (next.has(id)) next.delete(id);
    else next.add(id);
    persist(next);
  };

  const setMany = (ids: string[], value: boolean) => {
    const next = new Set(open);
    for (const id of ids) {
      if (value) next.add(id);
      else next.delete(id);
    }
    persist(next);
  };

  const allOpen = allIds.length > 0 && allIds.every((id) => open.has(id));
  const toggleAll = () => setMany(allIds, !allOpen);

  return { isOpen, toggle, setMany, allOpen, toggleAll };
}
