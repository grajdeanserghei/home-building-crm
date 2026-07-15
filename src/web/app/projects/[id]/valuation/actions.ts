"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { apiBaseUrl, type Currency } from "@/app/lib/api";
import { describeApiError } from "@/app/lib/errors";
import { t } from "@/app/lib/i18n";

// Parse a decimal form field to a number, defaulting to 0 for empty/NaN input.
function toNumber(value: FormDataEntryValue | null): number {
  const parsed = Number.parseFloat((value as string)?.trim() || "0");
  return Number.isNaN(parsed) ? 0 : parsed;
}

// The catalog header fields shared by create and update (surfaces, own-regie, reference).
function catalogHeaderPayload(formData: FormData) {
  return {
    catalogReference: (formData.get("catalogReference") as string)?.trim(),
    builtArea: toNumber(formData.get("builtArea")),
    grossFloorArea: toNumber(formData.get("grossFloorArea")),
    usableArea: toNumber(formData.get("usableArea")),
    ownRegieAdjustment: toNumber(formData.get("ownRegieAdjustment")),
  };
}

// Create the project's valuation catalog (CreateValuationCatalogCommand). Seeds VAT/currency/
// method on create; on success we return to the (now populated) hub where items are added.
export async function createValuationCatalog(formData: FormData) {
  const projectId = formData.get("projectId") as string;
  const header = catalogHeaderPayload(formData);
  if (!projectId || !header.catalogReference) return;

  const payload = {
    ...header,
    method: "SegregatedCost",
    currency: (formData.get("currency") as Currency) || "RON",
    vatRatePercentage: toNumber(formData.get("vatRatePercentage")),
  };

  const res = await fetch(
    `${apiBaseUrl()}/api/projects/${projectId}/valuation-catalog`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    },
  );

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/projects/${projectId}/valuation`);
  redirect(`/projects/${projectId}/valuation`);
}

// Edit the catalog header (UpdateValuationCatalogHeaderCommand). VAT is changed separately (it
// triggers a write-time recompute of every item's gross total — see changeValuationVat).
export async function updateValuationCatalog(formData: FormData) {
  const projectId = formData.get("projectId") as string;
  const catalogId = formData.get("catalogId") as string;
  const header = catalogHeaderPayload(formData);
  if (!projectId || !catalogId || !header.catalogReference) return;

  const res = await fetch(
    `${apiBaseUrl()}/api/valuation-catalogs/${catalogId}/header`,
    {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(header),
    },
  );

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/projects/${projectId}/valuation`);
  redirect(`/projects/${projectId}/valuation`);
}

// Promote a draft catalog to the project's active baseline. Stays on the hub.
export async function activateValuationCatalog(formData: FormData) {
  const projectId = formData.get("projectId") as string;
  const catalogId = formData.get("catalogId") as string;
  if (!projectId || !catalogId) return;

  const res = await fetch(
    `${apiBaseUrl()}/api/valuation-catalogs/${catalogId}/activate`,
    { method: "POST" },
  );

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/projects/${projectId}/valuation`);
}

// Change the report VAT rate (ChangeVatRateCommand { percentage }). The backend loops the items
// and recomputes each stored gross total — but leaves existing snapshots untouched (the
// deliberate asymmetry). Stays on the hub.
export async function changeValuationVat(formData: FormData) {
  const projectId = formData.get("projectId") as string;
  const catalogId = formData.get("catalogId") as string;
  if (!projectId || !catalogId) return;

  const res = await fetch(
    `${apiBaseUrl()}/api/valuation-catalogs/${catalogId}/vat-rate`,
    {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ percentage: toNumber(formData.get("vatRatePercentage")) }),
    },
  );

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/projects/${projectId}/valuation`);
}

// A priced-item payload shared by add and revise (Add/ReviseValuationItemCommand). The gross
// total is derived server-side from the catalog VAT rate, so it is not submitted. Money is built
// in the catalog currency.
function itemPayload(formData: FormData) {
  const currency = (formData.get("currency") as Currency) || "RON";
  return {
    sequence: Math.trunc(toNumber(formData.get("sequence"))),
    printedNumber: (formData.get("printedNumber") as string)?.trim(),
    name: (formData.get("name") as string)?.trim(),
    unit: (formData.get("unit") as string)?.trim(),
    catalogSource: (formData.get("catalogSource") as string)?.trim(),
    costWeight: toNumber(formData.get("costWeight")),
    unitCostPerBuiltArea: {
      amount: toNumber(formData.get("unitCostAmount")),
      currency,
    },
    totalCostWithoutVat: {
      amount: toNumber(formData.get("totalCostAmount")),
      currency,
    },
  };
}

export async function addValuationCatalogItem(formData: FormData) {
  const projectId = formData.get("projectId") as string;
  const catalogId = formData.get("catalogId") as string;
  const payload = itemPayload(formData);
  if (!projectId || !catalogId || !payload.name || !payload.printedNumber) return;

  const res = await fetch(
    `${apiBaseUrl()}/api/valuation-catalogs/${catalogId}/items`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    },
  );

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/projects/${projectId}/valuation`);
  redirect(`/projects/${projectId}/valuation`);
}

export async function reviseValuationCatalogItem(formData: FormData) {
  const projectId = formData.get("projectId") as string;
  const catalogId = formData.get("catalogId") as string;
  const itemId = formData.get("itemId") as string;
  const payload = itemPayload(formData);
  if (!projectId || !catalogId || !itemId || !payload.name || !payload.printedNumber)
    return;

  const res = await fetch(
    `${apiBaseUrl()}/api/valuation-catalogs/${catalogId}/items/${itemId}`,
    {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    },
  );

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/projects/${projectId}/valuation`);
  redirect(`/projects/${projectId}/valuation`);
}

// Soft-retire an item (kept for snapshot references). Stays on the hub.
export async function deactivateValuationCatalogItem(formData: FormData) {
  const projectId = formData.get("projectId") as string;
  const catalogId = formData.get("catalogId") as string;
  const itemId = formData.get("itemId") as string;
  if (!projectId || !catalogId || !itemId) return;

  const res = await fetch(
    `${apiBaseUrl()}/api/valuation-catalogs/${catalogId}/items/${itemId}/deactivate`,
    { method: "POST" },
  );

  if (!res.ok && res.status !== 404) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/projects/${projectId}/valuation`);
}

// POST a link/unlink for one catalog item and a (boqId, sectionId, subsectionId?) target
// (LinkBoqSectionCommand). The item id is in the route, so unlink needs the item that currently
// holds the target.
async function postLink(
  catalogId: string,
  itemId: string,
  target: { boqId: string; sectionId: string; subsectionId: string | null },
  remove: boolean,
): Promise<Response> {
  const suffix = remove ? "/links/remove" : "/links";
  return fetch(
    `${apiBaseUrl()}/api/valuation-catalogs/${catalogId}/items/${itemId}${suffix}`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(target),
    },
  );
}

// Map (or clear) the item behind a BoQ (boqId, sectionId, subsectionId?) triple. Bound to the
// auto-submitting select on the BoQ header:
//   - a chosen itemId links it (unlinking whatever item previously held the triple first, since
//     the domain rejects double-mapping — this gives replace-on-change);
//   - an empty itemId clears the current item's link for that triple.
// The invariants (no-double-count, granularity exclusivity) are enforced by the backend. We
// refresh the bid's BoQ page (the selects), the valuation hub and the comparison read model.
export async function setValuationLink(formData: FormData) {
  const projectId = formData.get("projectId") as string;
  const catalogId = formData.get("catalogId") as string;
  const boqId = formData.get("boqId") as string;
  const sectionId = formData.get("sectionId") as string;
  const subsectionId = (formData.get("subsectionId") as string)?.trim() || null;
  const itemId = (formData.get("itemId") as string)?.trim();
  const currentItemId = (formData.get("currentItemId") as string)?.trim();
  const bidId = (formData.get("bidId") as string)?.trim();
  if (!projectId || !catalogId || !boqId || !sectionId) return;
  // No change (re-selected the same item) — nothing to do.
  if (itemId === currentItemId) return;

  const target = { boqId, sectionId, subsectionId };

  // Replace-on-change: drop the triple's previous owner before linking the new item (the domain
  // rejects mapping a triple that another item already holds).
  if (currentItemId) {
    const res = await postLink(catalogId, currentItemId, target, true);
    if (!res.ok && res.status !== 404) {
      throw new Error(await describeApiError(res, "common.actionError"));
    }
  }

  if (itemId) {
    const res = await postLink(catalogId, itemId, target, false);
    if (!res.ok) {
      throw new Error(await describeApiError(res, "common.actionError"));
    }
  }

  if (bidId) revalidatePath(`/bids/${bidId}`);
  revalidatePath(`/projects/${projectId}/valuation`);
  revalidatePath(`/projects/${projectId}/valuation/vs-boq`);
}

// Record a dated snapshot against the catalog (CaptureConstructionValuationCommand). The server
// never parses the source file — the payload arrives already parsed (agent/MCP), keyed by
// `sourceContentHash` for idempotency (a duplicate hash resolves to the existing snapshot). Each
// item supplies only { valuationCatalogItemId, completionPercentage }; the money is frozen
// server-side from the catalog at capture. In the human UI this is a thin form.
export async function captureConstructionValuation(formData: FormData) {
  const projectId = formData.get("projectId") as string;
  const catalogId = formData.get("catalogId") as string;
  if (!projectId || !catalogId) return;

  const itemsRaw = (formData.get("items") as string)?.trim();
  let items: unknown = [];
  if (itemsRaw) {
    try {
      items = JSON.parse(itemsRaw);
    } catch {
      throw new Error(t("common.actionError", { error: "items" }));
    }
  }

  const assessedOn = (formData.get("assessedOn") as string)?.trim();
  const payload = {
    assessedOn,
    appraiser: (formData.get("appraiser") as string)?.trim() || null,
    sourceContentHash: (formData.get("sourceContentHash") as string)?.trim() || null,
    exchangeRate: {
      baseCurrency: "EUR",
      quoteCurrency: "RON",
      rate: toNumber(formData.get("ronPerEur")),
      asOf: assessedOn,
    },
    items,
  };

  const res = await fetch(
    `${apiBaseUrl()}/api/valuation-catalogs/${catalogId}/valuations`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    },
  );

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/projects/${projectId}/valuation/snapshots`);
  revalidatePath(`/projects/${projectId}/valuation/progress`);
  redirect(`/projects/${projectId}/valuation/snapshots`);
}
