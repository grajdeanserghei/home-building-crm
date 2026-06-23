"use server";

import { cookies } from "next/headers";
import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { apiBaseUrl, type ProjectStatus } from "./lib/api";
import { CURRENT_PROJECT_COOKIE } from "./lib/current-project";
import { describeApiError } from "./lib/errors";

// Scope the whole UI to a project chosen from the header switcher: persist the
// selection in a cookie and land on the dashboard ("/"), which renders in its
// context. `revalidatePath("/", "layout")` refreshes every page (and the header)
// so the new selection is reflected immediately.
export async function setCurrentProject(formData: FormData) {
  const id = (formData.get("projectId") as string) || "";
  const cookieStore = await cookies();
  if (id) {
    cookieStore.set(CURRENT_PROJECT_COOKIE, id, {
      path: "/",
      sameSite: "lax",
      maxAge: 60 * 60 * 24 * 365,
    });
  } else {
    cookieStore.delete(CURRENT_PROJECT_COOKIE);
  }
  revalidatePath("/", "layout");
  redirect("/");
}

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
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // The create form lives on its own route (/projects/new); return to the list.
  // Revalidate the whole tree so the header project switcher picks up the new project.
  revalidatePath("/", "layout");
  redirect("/projects");
}

export async function updateProject(formData: FormData) {
  const id = formData.get("id") as string;
  const name = (formData.get("name") as string)?.trim();
  if (!id || !name) return;

  const payload = {
    name,
    description: (formData.get("description") as string) || null,
    status: (formData.get("status") as ProjectStatus) || "Planned",
    dueDate: (formData.get("dueDate") as string) || null,
  };

  const res = await fetch(`${apiBaseUrl()}/api/projects/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // Refresh the list, then return to it (the edit form lives on its own route).
  // Layout-level revalidation keeps the header switcher's project name in sync.
  revalidatePath("/", "layout");
  redirect("/projects");
}

export async function deleteProject(formData: FormData) {
  const id = formData.get("id") as string;
  if (!id) return;

  const res = await fetch(`${apiBaseUrl()}/api/projects/${id}`, {
    method: "DELETE",
  });

  if (!res.ok && res.status !== 404) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // Layout-level revalidation drops the deleted project from the header switcher too.
  revalidatePath("/", "layout");
}
