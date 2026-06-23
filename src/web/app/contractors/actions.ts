"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { apiBaseUrl } from "../lib/api";
import { describeApiError } from "../lib/errors";

// Build the JSON body shared by register and update — the two backend commands are
// identical. Contact and address are sent as nested objects; the backend collapses
// an all-empty one to null, so emitting them unconditionally is fine.
function contractorPayload(formData: FormData) {
  const str = (key: string) => (formData.get(key) as string)?.trim() || null;
  return {
    name: (formData.get("name") as string)?.trim(),
    fiscalCode: str("fiscalCode"),
    registrationNumber: str("registrationNumber"),
    contact: {
      personName: str("personName"),
      email: str("email"),
      phone: str("phone"),
    },
    address: {
      street: str("street"),
      city: str("city"),
      county: str("county"),
      postalCode: str("postalCode"),
      country: str("country"),
    },
    notes: str("notes"),
    // Trades are managed incrementally on the detail page (addContractorTrade /
    // removeContractorTrade), so they are intentionally omitted here — a contractor update with
    // no tradeIds field leaves the firm's trades untouched.
  };
}

export async function registerContractor(formData: FormData) {
  const payload = contractorPayload(formData);
  if (!payload.name) return;

  const res = await fetch(`${apiBaseUrl()}/api/contractors`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath("/contractors");
}

export async function updateContractor(formData: FormData) {
  const id = formData.get("id") as string;
  const payload = contractorPayload(formData);
  if (!id || !payload.name) return;

  const res = await fetch(`${apiBaseUrl()}/api/contractors/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // Refresh the list, then return to it (the edit form lives on its own route).
  revalidatePath("/contractors");
  redirect("/contractors");
}

// Tag the contractor with one trade it performs (idempotent server-side). The select offers only
// active, not-yet-assigned trades; an unknown/inactive trade comes back as a 400.
export async function addContractorTrade(formData: FormData) {
  const id = formData.get("id") as string;
  const tradeId = formData.get("tradeId") as string;
  if (!id || !tradeId) return;

  const res = await fetch(
    `${apiBaseUrl()}/api/contractors/${id}/trades/${tradeId}`,
    { method: "POST" },
  );

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/contractors/${id}`);
}

// Remove one trade tag (idempotent). A 404 (already gone) is treated as success.
export async function removeContractorTrade(formData: FormData) {
  const id = formData.get("id") as string;
  const tradeId = formData.get("tradeId") as string;
  if (!id || !tradeId) return;

  const res = await fetch(
    `${apiBaseUrl()}/api/contractors/${id}/trades/${tradeId}`,
    { method: "DELETE" },
  );

  if (!res.ok && res.status !== 404) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/contractors/${id}`);
}

export async function deleteContractor(formData: FormData) {
  const id = formData.get("id") as string;
  if (!id) return;

  const res = await fetch(`${apiBaseUrl()}/api/contractors/${id}`, {
    method: "DELETE",
  });

  if (!res.ok && res.status !== 404) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath("/contractors");
}
