"use server";

import { revalidatePath } from "next/cache";
import { apiBaseUrl, type ProjectStatus } from "./lib/api";

export async function createProject(formData: FormData) {
  const name = (formData.get("name") as string)?.trim();
  if (!name) return;

  const payload = {
    name,
    description: (formData.get("description") as string) || null,
    status: (formData.get("status") as ProjectStatus) || "Planned",
    dueDate: (formData.get("dueDate") as string) || null,
  };

  const res = await fetch(`${apiBaseUrl()}/api/projects`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error(`Failed to create project: ${res.status}`);
  }

  revalidatePath("/");
}

export async function deleteProject(formData: FormData) {
  const id = formData.get("id") as string;
  if (!id) return;

  const res = await fetch(`${apiBaseUrl()}/api/projects/${id}`, {
    method: "DELETE",
  });

  if (!res.ok && res.status !== 404) {
    throw new Error(`Failed to delete project: ${res.status}`);
  }

  revalidatePath("/");
}
