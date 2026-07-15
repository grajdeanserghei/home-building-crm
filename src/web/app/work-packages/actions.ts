"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import {
  apiBaseUrl,
  type ScopeItemRequirement,
  type WorkPackage,
  type WorkPackageStatus,
} from "../lib/api";
import { describeApiError } from "@/app/lib/errors";

// Build the JSON body shared by define and update. Status is intentionally absent:
// the backend command omits it (lifecycle transitions live on dedicated endpoints).
function workPackagePayload(formData: FormData) {
  const sequenceRaw = (formData.get("sequence") as string)?.trim();
  const sequence = sequenceRaw ? Number.parseInt(sequenceRaw, 10) : 0;
  return {
    name: (formData.get("name") as string)?.trim(),
    description: (formData.get("description") as string) || null,
    sequence: Number.isNaN(sequence) ? 0 : sequence,
    plannedStartDate: (formData.get("plannedStartDate") as string) || null,
    plannedEndDate: (formData.get("plannedEndDate") as string) || null,
    // Required trades are managed incrementally on the detail page (addRequiredTrade /
    // removeRequiredTrade), so they are intentionally omitted here — an update with no
    // requiredTradeIds field leaves the package's trades untouched.
  };
}

export async function defineWorkPackage(formData: FormData) {
  const projectId = formData.get("projectId") as string;
  const payload = workPackagePayload(formData);
  if (!projectId || !payload.name) return;

  const res = await fetch(
    `${apiBaseUrl()}/api/projects/${projectId}/work-packages`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    },
  );

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // Defining a package is a step away on its own route; revalidate and return to the project.
  revalidatePath(`/projects/${projectId}`);
  redirect(`/projects/${projectId}`);
}

export async function updateWorkPackage(formData: FormData) {
  const id = formData.get("id") as string;
  const projectId = formData.get("projectId") as string;
  const payload = workPackagePayload(formData);
  if (!id || !payload.name) return;

  const res = await fetch(`${apiBaseUrl()}/api/work-packages/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // Refresh the project's list, then return to it (the edit form is its own route).
  revalidatePath(`/projects/${projectId}`);
  redirect(`/projects/${projectId}`);
}

export async function deleteWorkPackage(formData: FormData) {
  const id = formData.get("id") as string;
  const projectId = formData.get("projectId") as string;
  if (!id) return;

  const res = await fetch(`${apiBaseUrl()}/api/work-packages/${id}`, {
    method: "DELETE",
  });

  if (!res.ok && res.status !== 404) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // Deleting can be driven from the package's own read view, the project overview, or the
  // home dashboard (which also lists work packages) — revalidate both the project's list and
  // the home page, then return to the project (the gone package's detail page no longer exists).
  revalidatePath(`/projects/${projectId}`);
  revalidatePath("/");
  redirect(`/projects/${projectId}`);
}

// Reorder -----------------------------------------------------------------

// Outcome of a drag-and-drop reorder: on success the project's packages freshly renumbered (so the
// sortable table reconciles to canonical 1..n order), otherwise a localized message to surface and
// revert the optimistic move.
export type ReorderWorkPackagesResult =
  | { ok: true; workPackages: WorkPackage[] }
  | { ok: false; error: string };

// Reorder a project's work packages. Called programmatically by the sortable table (not a form) —
// one call per drop, carrying the full ordered id list. Returns the renumbered packages rather than
// redirecting; `revalidatePath` keeps the project overview (and its shared home dashboard) fresh.
export async function reorderWorkPackages(input: {
  projectId: string;
  orderedIds: string[];
}): Promise<ReorderWorkPackagesResult> {
  const res = await fetch(
    `${apiBaseUrl()}/api/projects/${input.projectId}/work-packages/reorder`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ orderedWorkPackageIds: input.orderedIds }),
    },
  );

  if (!res.ok) {
    return { ok: false, error: await describeApiError(res, "common.actionError") };
  }

  const workPackages = (await res.json()) as WorkPackage[];
  revalidatePath(`/projects/${input.projectId}`);
  return { ok: true, workPackages };
}

// Lifecycle ---------------------------------------------------------------

// Move a work package through its lifecycle (Defined → Open for Bids → … → Completed /
// Cancelled). Awarding goes through the award flow, not here; the backend rejects an
// illegal transition (e.g. starting an un-awarded package) with a 409.
export async function changeWorkPackageStatus(formData: FormData) {
  const id = formData.get("id") as string;
  const projectId = formData.get("projectId") as string;
  const status = formData.get("status") as WorkPackageStatus;
  if (!id || !status) return;

  const res = await fetch(`${apiBaseUrl()}/api/work-packages/${id}/status`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ status }),
  });

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/work-packages/${id}`);
  // The project's list shows each package's status badge — keep it fresh too.
  if (projectId) {
    revalidatePath(`/projects/${projectId}`);
  }
  // The change is driven from the dedicated status route; return to the package's read view.
  redirect(`/work-packages/${id}`);
}

// Scope items -------------------------------------------------------------

export async function addScopeItem(formData: FormData) {
  const workPackageId = formData.get("workPackageId") as string;
  const name = (formData.get("name") as string)?.trim();
  const requirement = formData.get("requirement") as ScopeItemRequirement;
  if (!workPackageId || !name || !requirement) return;

  const sequenceRaw = (formData.get("sequence") as string)?.trim();
  const sequence = sequenceRaw ? Number.parseInt(sequenceRaw, 10) : 0;

  const payload = {
    name,
    requirement,
    description: (formData.get("description") as string)?.trim() || null,
    sequence: Number.isNaN(sequence) ? 0 : sequence,
  };

  const res = await fetch(
    `${apiBaseUrl()}/api/work-packages/${workPackageId}/scope-items`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    },
  );

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // Adding a scope item is a step away on its own route; revalidate and return to the package.
  revalidatePath(`/work-packages/${workPackageId}`);
  redirect(`/work-packages/${workPackageId}`);
}

export async function removeScopeItem(formData: FormData) {
  const workPackageId = formData.get("workPackageId") as string;
  const scopeItemId = formData.get("scopeItemId") as string;
  if (!workPackageId || !scopeItemId) return;

  const res = await fetch(
    `${apiBaseUrl()}/api/work-packages/${workPackageId}/scope-items/${scopeItemId}`,
    { method: "DELETE" },
  );

  if (!res.ok && res.status !== 404) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/work-packages/${workPackageId}`);
}

// Required trades ---------------------------------------------------------

// Require one trade for the package (idempotent server-side). The select offers only active,
// not-yet-required trades; an unknown/inactive trade comes back as a 400.
export async function addRequiredTrade(formData: FormData) {
  const workPackageId = formData.get("workPackageId") as string;
  const tradeId = formData.get("tradeId") as string;
  if (!workPackageId || !tradeId) return;

  const res = await fetch(
    `${apiBaseUrl()}/api/work-packages/${workPackageId}/trades/${tradeId}`,
    { method: "POST" },
  );

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/work-packages/${workPackageId}`);
}

// Drop one required trade (idempotent). A 404 (already gone) is treated as success.
export async function removeRequiredTrade(formData: FormData) {
  const workPackageId = formData.get("workPackageId") as string;
  const tradeId = formData.get("tradeId") as string;
  if (!workPackageId || !tradeId) return;

  const res = await fetch(
    `${apiBaseUrl()}/api/work-packages/${workPackageId}/trades/${tradeId}`,
    { method: "DELETE" },
  );

  if (!res.ok && res.status !== 404) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/work-packages/${workPackageId}`);
}
