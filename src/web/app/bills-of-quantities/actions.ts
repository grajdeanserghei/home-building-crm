"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { apiBaseUrl, type BoqStatus, type Currency } from "../lib/api";
import { describeApiError } from "@/app/lib/errors";

// An <input type="date"> yields a bare `yyyy-MM-dd`. The BoQ's submittedOn / validUntil
// are DateTimeOffset on the backend, whose System.Text.Json converter wants a full ISO
// timestamp — so pin the date to midnight UTC. Empty → null.
function toDateTime(value: FormDataEntryValue | null): string | null {
  const raw = (value as string)?.trim();
  return raw ? `${raw}T00:00:00Z` : null;
}

// Build the optional pinned exchange rate from the form. We model it as "1 EUR = rate RON"
// (base EUR, quote RON) — with only two currencies this always involves the pricing
// currency, satisfying the aggregate's invariant whichever one it is. A blank rate means
// no pinned rate (null). `asOf` (a DateOnly server-side) defaults to today when omitted.
function buildExchangeRate(formData: FormData) {
  const rateRaw = (formData.get("exchangeRate") as string)?.trim();
  if (!rateRaw) return null;

  const rate = Number.parseFloat(rateRaw);
  if (Number.isNaN(rate) || rate <= 0) return null;

  const asOf =
    (formData.get("exchangeRateAsOf") as string)?.trim() ||
    new Date().toISOString().slice(0, 10);

  return { baseCurrency: "EUR", quoteCurrency: "RON", rate, asOf };
}

// The header fields shared by draft and update. The pricing currency is fixed at draft
// time, so it is not part of the update payload (the backend's update command omits it).
function headerPayload(formData: FormData) {
  return {
    reference: (formData.get("reference") as string)?.trim() || null,
    exchangeRate: buildExchangeRate(formData),
    submittedOn: toDateTime(formData.get("submittedOn")),
    validUntil: toDateTime(formData.get("validUntil")),
  };
}

// Draft a new BoQ version within a bid. The collection is nested under the bid; the
// version number is assigned server-side. On success we jump straight to the new BoQ.
export async function draftBoq(formData: FormData) {
  const bidId = formData.get("bidId") as string;
  const pricingCurrency = formData.get("pricingCurrency") as Currency;
  if (!bidId || !pricingCurrency) return;

  const payload = { pricingCurrency, ...headerPayload(formData) };

  const res = await fetch(
    `${apiBaseUrl()}/api/bids/${bidId}/bills-of-quantities`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    },
  );

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  const created = await res.json();
  revalidatePath(`/bids/${bidId}`);
  redirect(`/bills-of-quantities/${created.id}`);
}

// Update a BoQ's header (reference, pinned rate, dates). Pricing currency is immutable.
export async function updateBoq(formData: FormData) {
  const id = formData.get("id") as string;
  if (!id) return;

  const res = await fetch(`${apiBaseUrl()}/api/bills-of-quantities/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(headerPayload(formData)),
  });

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // Refresh the detail, then return to it (the edit form is its own route).
  revalidatePath(`/bills-of-quantities/${id}`);
  redirect(`/bills-of-quantities/${id}`);
}

// Move a BoQ through its lifecycle (Draft → Submitted → Accepted / Rejected / Withdrawn).
// Rejected and Withdrawn are terminal; the backend rejects transitions out of them (409).
export async function changeBoqStatus(formData: FormData) {
  const id = formData.get("id") as string;
  const bidId = formData.get("bidId") as string;
  const status = formData.get("status") as BoqStatus;
  if (!id || !status) return;

  const res = await fetch(
    `${apiBaseUrl()}/api/bills-of-quantities/${id}/status`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ status }),
    },
  );

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/bills-of-quantities/${id}`);
  if (bidId) {
    revalidatePath(`/bids/${bidId}`);
  }
}

export async function deleteBoq(formData: FormData) {
  const id = formData.get("id") as string;
  const bidId = formData.get("bidId") as string;
  if (!id) return;

  const res = await fetch(`${apiBaseUrl()}/api/bills-of-quantities/${id}`, {
    method: "DELETE",
  });

  if (!res.ok && res.status !== 404) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // The detail page is gone; return to the bid's BoQ list.
  if (bidId) {
    revalidatePath(`/bids/${bidId}`);
    redirect(`/bids/${bidId}`);
  }
}

// Sections ----------------------------------------------------------------

export async function addSection(formData: FormData) {
  const boqId = formData.get("boqId") as string;
  const name = (formData.get("name") as string)?.trim();
  if (!boqId || !name) return;

  const sequenceRaw = (formData.get("sequence") as string)?.trim();
  const sequence = sequenceRaw ? Number.parseInt(sequenceRaw, 10) : 0;

  const payload = {
    name,
    sequence: Number.isNaN(sequence) ? 0 : sequence,
    description: (formData.get("description") as string)?.trim() || null,
  };

  const res = await fetch(
    `${apiBaseUrl()}/api/bills-of-quantities/${boqId}/sections`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    },
  );

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/bills-of-quantities/${boqId}`);
}

export async function removeSection(formData: FormData) {
  const boqId = formData.get("boqId") as string;
  const sectionId = formData.get("sectionId") as string;
  if (!boqId || !sectionId) return;

  const res = await fetch(
    `${apiBaseUrl()}/api/bills-of-quantities/${boqId}/sections/${sectionId}`,
    { method: "DELETE" },
  );

  if (!res.ok && res.status !== 404) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/bills-of-quantities/${boqId}`);
}

// Line items --------------------------------------------------------------

export async function addLineItem(formData: FormData) {
  const boqId = formData.get("boqId") as string;
  const sectionId = formData.get("sectionId") as string;
  const description = (formData.get("description") as string)?.trim();
  const unitOfMeasureId = formData.get("unitOfMeasureId") as string;
  if (!boqId || !sectionId || !description || !unitOfMeasureId) return;

  const quantity = Number.parseFloat(
    (formData.get("quantity") as string)?.trim() || "0",
  );
  const amount = Number.parseFloat(
    (formData.get("unitPriceAmount") as string)?.trim() || "0",
  );
  // The VAT rate (percent). Blank → null, letting the backend apply the standard 21%.
  const vatRaw = (formData.get("vatRatePercentage") as string)?.trim();
  const vatRatePercentage =
    vatRaw === undefined || vatRaw === "" ? null : Number.parseFloat(vatRaw);
  const sequenceRaw = (formData.get("sequence") as string)?.trim();
  const sequence = sequenceRaw ? Number.parseInt(sequenceRaw, 10) : 0;
  // The line price is always in the BoQ's pricing currency (carried as a hidden field).
  const currency = formData.get("currency") as Currency;

  const payload = {
    description,
    quantity: Number.isNaN(quantity) ? 0 : quantity,
    unitOfMeasureId,
    unitPrice: { amount: Number.isNaN(amount) ? 0 : amount, currency },
    vatRatePercentage:
      vatRatePercentage === null || Number.isNaN(vatRatePercentage)
        ? null
        : vatRatePercentage,
    sequence: Number.isNaN(sequence) ? 0 : sequence,
    notes: (formData.get("notes") as string)?.trim() || null,
  };

  const res = await fetch(
    `${apiBaseUrl()}/api/bills-of-quantities/${boqId}/sections/${sectionId}/line-items`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    },
  );

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/bills-of-quantities/${boqId}`);
}

export async function removeLineItem(formData: FormData) {
  const boqId = formData.get("boqId") as string;
  const sectionId = formData.get("sectionId") as string;
  const lineItemId = formData.get("lineItemId") as string;
  if (!boqId || !sectionId || !lineItemId) return;

  const res = await fetch(
    `${apiBaseUrl()}/api/bills-of-quantities/${boqId}/sections/${sectionId}/line-items/${lineItemId}`,
    { method: "DELETE" },
  );

  if (!res.ok && res.status !== 404) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/bills-of-quantities/${boqId}`);
}
