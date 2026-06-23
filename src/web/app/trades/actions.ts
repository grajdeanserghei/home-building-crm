"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { apiBaseUrl } from "../lib/api";
import { describeApiError } from "../lib/errors";
import { t } from "../lib/i18n";

export async function defineTrade(formData: FormData) {
  const name = (formData.get("name") as string)?.trim();
  if (!name) return;

  const payload = {
    name,
    code: (formData.get("code") as string)?.trim() || null,
  };

  const res = await fetch(`${apiBaseUrl()}/api/trades`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (res.status === 409) {
    throw new Error(t("trades.nameExists", { name }));
  }
  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath("/trades");
}

export async function updateTrade(formData: FormData) {
  const id = formData.get("id") as string;
  const name = (formData.get("name") as string)?.trim();
  if (!id || !name) return;

  const payload = {
    name,
    code: (formData.get("code") as string)?.trim() || null,
  };

  const res = await fetch(`${apiBaseUrl()}/api/trades/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // Refresh the list, then return to it (the edit form lives on its own route).
  revalidatePath("/trades");
  redirect("/trades");
}

// Retire/restore a trade. There is no delete endpoint; a trade is taken out of use by
// deactivating it (and brought back by activating it). `isActive` carries the target
// state, set by the toggle to the opposite of the current one.
export async function setTradeActive(formData: FormData) {
  const id = formData.get("id") as string;
  if (!id) return;

  const activate = formData.get("isActive") === "true";
  const verb = activate ? "activate" : "deactivate";

  const res = await fetch(`${apiBaseUrl()}/api/trades/${id}/${verb}`, {
    method: "POST",
  });

  if (!res.ok && res.status !== 404) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath("/trades");
}
