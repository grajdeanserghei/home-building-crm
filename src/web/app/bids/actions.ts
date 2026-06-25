"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { apiBaseUrl, type BidStatus, type NoteType } from "../lib/api";
import { describeApiError } from "@/app/lib/errors";

// Open a new bid for a contractor on a work package. The collection is nested under
// the work package, so the contractor (and optional first contact / summary) is the
// body and the work-package id is carried as a hidden field for routing.
export async function openBid(formData: FormData) {
  const workPackageId = formData.get("workPackageId") as string;
  const contractorId = (formData.get("contractorId") as string)?.trim();
  if (!workPackageId || !contractorId) return;

  const payload = {
    contractorId,
    firstContactedOn: (formData.get("firstContactedOn") as string) || null,
    summary: (formData.get("summary") as string) || null,
    label: (formData.get("label") as string) || null,
  };

  const res = await fetch(
    `${apiBaseUrl()}/api/work-packages/${workPackageId}/bids`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    },
  );

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // Opening a bid is a step away on its own route; revalidate and return to the work package.
  revalidatePath(`/work-packages/${workPackageId}`);
  redirect(`/work-packages/${workPackageId}`);
}

// Update a bid's free-text standing. Status is intentionally absent: the backend
// command omits it (lifecycle transitions live on the dedicated status endpoint).
export async function updateBid(formData: FormData) {
  const id = formData.get("id") as string;
  if (!id) return;

  const payload = {
    summary: (formData.get("summary") as string) || null,
    firstContactedOn: (formData.get("firstContactedOn") as string) || null,
    label: (formData.get("label") as string) || null,
  };

  const res = await fetch(`${apiBaseUrl()}/api/bids/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // Refresh the bid detail, then return to it (the edit form is its own route).
  revalidatePath(`/bids/${id}`);
  redirect(`/bids/${id}`);
}

// Duplicate a bid in place: the backend clones it for the same contractor on the same work
// package (a new offer, e.g. a "Buget" variant of a "Premium" one) with a fresh discussion log,
// then we jump to the copy so it can be tweaked.
export async function duplicateBid(formData: FormData) {
  const id = formData.get("id") as string;
  if (!id) return;

  const res = await fetch(`${apiBaseUrl()}/api/bids/${id}/duplicate`, {
    method: "POST",
  });

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  const created = (await res.json()) as { id: string; workPackageId: string };
  revalidatePath(`/work-packages/${created.workPackageId}`);
  redirect(`/bids/${created.id}`);
}

// Move a bid through its lifecycle. Selecting a winner rejects the rival bids on the
// same work package server-side, so the work-package list is revalidated too.
export async function changeBidStatus(formData: FormData) {
  const id = formData.get("id") as string;
  const status = formData.get("status") as BidStatus;
  const workPackageId = formData.get("workPackageId") as string;
  if (!id || !status) return;

  const res = await fetch(`${apiBaseUrl()}/api/bids/${id}/status`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ status }),
  });

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/bids/${id}`);
  if (workPackageId) {
    revalidatePath(`/work-packages/${workPackageId}`);
  }
  // The change is driven from the dedicated status route; return to the bid's read view.
  redirect(`/bids/${id}`);
}

export async function deleteBid(formData: FormData) {
  const id = formData.get("id") as string;
  const workPackageId = formData.get("workPackageId") as string;
  if (!id) return;

  const res = await fetch(`${apiBaseUrl()}/api/bids/${id}`, {
    method: "DELETE",
  });

  if (!res.ok && res.status !== 404) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // The bid detail page is gone; return to the work package's bid list.
  if (workPackageId) {
    revalidatePath(`/work-packages/${workPackageId}`);
    redirect(`/work-packages/${workPackageId}`);
  }
}

// Append a dated interaction to a bid's discussion log. The author is the current
// user (filled in server-side), so it is not part of the body.
export async function logBidNote(formData: FormData) {
  const bidId = formData.get("bidId") as string;
  const content = (formData.get("content") as string)?.trim();
  const occurredOn = (formData.get("occurredOn") as string)?.trim();
  if (!bidId || !content || !occurredOn) return;

  const payload = {
    type: (formData.get("type") as NoteType) || "Note",
    occurredOn,
    content,
  };

  const res = await fetch(`${apiBaseUrl()}/api/bids/${bidId}/notes`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // Logging is a step away on its own route; revalidate and return to the bid's read view.
  revalidatePath(`/bids/${bidId}`);
  redirect(`/bids/${bidId}`);
}

// Append a note to a bid from the project-wide offers overview. Unlike `logBidNote`
// it does not redirect to the bid: the owner is triaging offers on the overview (call,
// then jot down when a quote is due), so it just revalidates that page and stays put.
export async function logBidNoteOnProject(formData: FormData) {
  const bidId = formData.get("bidId") as string;
  const projectId = formData.get("projectId") as string;
  const content = (formData.get("content") as string)?.trim();
  const occurredOn = (formData.get("occurredOn") as string)?.trim();
  if (!bidId || !content || !occurredOn) return;

  const payload = {
    type: (formData.get("type") as NoteType) || "Note",
    occurredOn,
    content,
  };

  const res = await fetch(`${apiBaseUrl()}/api/bids/${bidId}/notes`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // Stay on the overview (no redirect); refresh it so the new note appears, and the
  // bid's own page if it's visited next.
  if (projectId) {
    revalidatePath(`/projects/${projectId}/bids`);
  }
  revalidatePath(`/bids/${bidId}`);
}

export async function removeBidNote(formData: FormData) {
  const bidId = formData.get("bidId") as string;
  const noteId = formData.get("noteId") as string;
  if (!bidId || !noteId) return;

  const res = await fetch(
    `${apiBaseUrl()}/api/bids/${bidId}/notes/${noteId}`,
    { method: "DELETE" },
  );

  if (!res.ok && res.status !== 404) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/bids/${bidId}`);
}
