"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { apiBaseUrl, type BidStatus, type NoteType } from "../lib/api";

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
    throw new Error(`Failed to open bid: ${res.status}`);
  }

  revalidatePath(`/work-packages/${workPackageId}`);
}

// Update a bid's free-text standing. Status is intentionally absent: the backend
// command omits it (lifecycle transitions live on the dedicated status endpoint).
export async function updateBid(formData: FormData) {
  const id = formData.get("id") as string;
  if (!id) return;

  const payload = {
    summary: (formData.get("summary") as string) || null,
    firstContactedOn: (formData.get("firstContactedOn") as string) || null,
  };

  const res = await fetch(`${apiBaseUrl()}/api/bids/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error(`Failed to update bid: ${res.status}`);
  }

  // Refresh the bid detail, then return to it (the edit form is its own route).
  revalidatePath(`/bids/${id}`);
  redirect(`/bids/${id}`);
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
    throw new Error(`Failed to change bid status: ${res.status}`);
  }

  revalidatePath(`/bids/${id}`);
  if (workPackageId) {
    revalidatePath(`/work-packages/${workPackageId}`);
  }
}

export async function deleteBid(formData: FormData) {
  const id = formData.get("id") as string;
  const workPackageId = formData.get("workPackageId") as string;
  if (!id) return;

  const res = await fetch(`${apiBaseUrl()}/api/bids/${id}`, {
    method: "DELETE",
  });

  if (!res.ok && res.status !== 404) {
    throw new Error(`Failed to delete bid: ${res.status}`);
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
    throw new Error(`Failed to log note: ${res.status}`);
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
    throw new Error(`Failed to remove note: ${res.status}`);
  }

  revalidatePath(`/bids/${bidId}`);
}
