"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { apiBaseUrl, type UnitCategory } from "../lib/api";

// Parse the comma-separated aliases field into a clean list. The backend
// normalises (trims + lowercases) each alias and drops ones equal to the code,
// so emitting the raw tokens is fine.
function parseAliases(formData: FormData): string[] {
  return ((formData.get("aliases") as string) ?? "")
    .split(",")
    .map((a) => a.trim())
    .filter((a) => a.length > 0);
}

export async function defineUnitOfMeasure(formData: FormData) {
  const code = (formData.get("code") as string)?.trim();
  const name = (formData.get("name") as string)?.trim();
  if (!code || !name) return;

  const payload = {
    code,
    name,
    category: (formData.get("category") as UnitCategory) || "Other",
    aliases: parseAliases(formData),
  };

  const res = await fetch(`${apiBaseUrl()}/api/units-of-measure`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (res.status === 409) {
    throw new Error(`A unit of measure with code "${code}" already exists.`);
  }
  if (!res.ok) {
    throw new Error(`Failed to define unit of measure: ${res.status}`);
  }

  revalidatePath("/units-of-measure");
}

export async function updateUnitOfMeasure(formData: FormData) {
  const id = formData.get("id") as string;
  const name = (formData.get("name") as string)?.trim();
  if (!id || !name) return;

  // Code is immutable, so the update command carries only name/category/aliases.
  const payload = {
    name,
    category: (formData.get("category") as UnitCategory) || "Other",
    aliases: parseAliases(formData),
  };

  const res = await fetch(`${apiBaseUrl()}/api/units-of-measure/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error(`Failed to update unit of measure: ${res.status}`);
  }

  // Refresh the list, then return to it (the edit form lives on its own route).
  revalidatePath("/units-of-measure");
  redirect("/units-of-measure");
}

// Retire/restore a unit of measure. There is no delete endpoint; a unit is taken
// out of use by deactivating it (and brought back by activating it). `isActive`
// carries the target state, set by the toggle to the opposite of the current one.
export async function setUnitOfMeasureActive(formData: FormData) {
  const id = formData.get("id") as string;
  if (!id) return;

  const activate = formData.get("isActive") === "true";
  const verb = activate ? "activate" : "deactivate";

  const res = await fetch(
    `${apiBaseUrl()}/api/units-of-measure/${id}/${verb}`,
    { method: "POST" },
  );

  if (!res.ok && res.status !== 404) {
    throw new Error(`Failed to ${verb} unit of measure: ${res.status}`);
  }

  revalidatePath("/units-of-measure");
}
