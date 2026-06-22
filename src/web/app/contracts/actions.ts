"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { apiBaseUrl, type ContractStatus, type Currency } from "../lib/api";

// An <input type="date"> yields a bare `yyyy-MM-dd`. A contract's signed/start/end
// dates are DateTimeOffset on the backend, whose System.Text.Json converter wants a
// full ISO timestamp — so pin the date to midnight UTC. Empty → null.
function toDateTime(value: FormDataEntryValue | null): string | null {
  const raw = (value as string)?.trim();
  return raw ? `${raw}T00:00:00Z` : null;
}

// Build a Money value from an amount field + currency. Returns null when the amount is
// blank/invalid, which (on award) lets the backend default the value to the BoQ total.
function buildValue(formData: FormData): {
  amount: number;
  currency: Currency;
} | null {
  const raw = (formData.get("valueAmount") as string)?.trim();
  if (!raw) return null;
  const amount = Number.parseFloat(raw);
  if (Number.isNaN(amount)) return null;
  const currency = (formData.get("valueCurrency") as Currency) || "RON";
  return { amount, currency };
}

// Award a contract from a chosen winning BoQ. The award is atomic server-side: it
// accepts the BoQ, selects its bid (rejecting the rivals), creates the contract, and
// transitions the work package to Awarded. Value is optional (defaults to the BoQ
// total). The owning bid/work-package ids ride along as hidden fields so their pages
// can be revalidated. On success we jump straight to the new contract.
export async function awardContract(formData: FormData) {
  const boqId = formData.get("boqId") as string;
  if (!boqId) return;

  const payload = {
    boqId,
    value: buildValue(formData),
    contractNumber: (formData.get("contractNumber") as string)?.trim() || null,
    startDate: toDateTime(formData.get("startDate")),
    plannedEndDate: toDateTime(formData.get("plannedEndDate")),
    notes: (formData.get("notes") as string)?.trim() || null,
  };

  const res = await fetch(`${apiBaseUrl()}/api/contracts`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error(`Failed to award contract: ${res.status}`);
  }

  const created = await res.json();

  // The award changed the BoQ, its bid and the work package; refresh whichever we know.
  revalidatePath(`/bills-of-quantities/${boqId}`);
  const bidId = formData.get("bidId") as string;
  const workPackageId = formData.get("workPackageId") as string;
  if (bidId) revalidatePath(`/bids/${bidId}`);
  if (workPackageId) revalidatePath(`/work-packages/${workPackageId}`);

  redirect(`/contracts/${created.id}`);
}

// Update a contract's header details (reference number, agreed value, planned dates,
// notes). The awarded work package and accepted BoQ are fixed and not part of the body.
export async function updateContract(formData: FormData) {
  const id = formData.get("id") as string;
  if (!id) return;

  const value = buildValue(formData);

  const payload = {
    contractNumber: (formData.get("contractNumber") as string)?.trim() || null,
    // Value is required on update; fall back to a zero amount in the contract's
    // currency if the field was somehow cleared (the backend rejects null).
    value: value ?? {
      amount: 0,
      currency: (formData.get("valueCurrency") as Currency) || "RON",
    },
    startDate: toDateTime(formData.get("startDate")),
    plannedEndDate: toDateTime(formData.get("plannedEndDate")),
    notes: (formData.get("notes") as string)?.trim() || null,
  };

  const res = await fetch(`${apiBaseUrl()}/api/contracts/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error(`Failed to update contract: ${res.status}`);
  }

  // Refresh the detail, then return to it (the edit form is its own route).
  revalidatePath(`/contracts/${id}`);
  redirect(`/contracts/${id}`);
}

// Move a contract through its lifecycle (Draft → Signed → Active → Completed /
// Terminated). SignedOn is required when moving to Signed; ActualEndDate when moving to
// Completed — both are sent as optional fields and validated by the backend.
export async function changeContractStatus(formData: FormData) {
  const id = formData.get("id") as string;
  const status = formData.get("status") as ContractStatus;
  if (!id || !status) return;

  const payload = {
    status,
    signedOn: toDateTime(formData.get("signedOn")),
    actualEndDate: toDateTime(formData.get("actualEndDate")),
  };

  const res = await fetch(`${apiBaseUrl()}/api/contracts/${id}/status`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error(`Failed to change contract status: ${res.status}`);
  }

  revalidatePath(`/contracts/${id}`);
}

export async function deleteContract(formData: FormData) {
  const id = formData.get("id") as string;
  if (!id) return;

  const res = await fetch(`${apiBaseUrl()}/api/contracts/${id}`, {
    method: "DELETE",
  });

  if (!res.ok && res.status !== 404) {
    throw new Error(`Failed to delete contract: ${res.status}`);
  }

  // The detail page is gone; return to the contracts list.
  revalidatePath("/contracts");
  redirect("/contracts");
}
