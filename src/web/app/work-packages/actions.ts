"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { apiBaseUrl } from "../lib/api";

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
    throw new Error(`Failed to define work package: ${res.status}`);
  }

  revalidatePath(`/projects/${projectId}`);
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
    throw new Error(`Failed to update work package: ${res.status}`);
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
    throw new Error(`Failed to delete work package: ${res.status}`);
  }

  revalidatePath(`/projects/${projectId}`);
}
