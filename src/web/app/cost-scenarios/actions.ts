"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { apiBaseUrl } from "../lib/api";
import { describeApiError } from "@/app/lib/errors";

// Header fields shared by create and update.
function headerPayload(formData: FormData) {
  return {
    name: (formData.get("name") as string)?.trim(),
    description: (formData.get("description") as string)?.trim() || null,
  };
}

// Create a scenario in a project. On success we jump straight to its (empty) detail page,
// where bids are chosen per work package.
export async function createCostScenario(formData: FormData) {
  const projectId = formData.get("projectId") as string;
  const payload = headerPayload(formData);
  if (!projectId || !payload.name) return;

  const res = await fetch(
    `${apiBaseUrl()}/api/projects/${projectId}/cost-scenarios`,
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
  revalidatePath(`/projects/${projectId}/cost-scenarios`);
  redirect(`/projects/${projectId}/cost-scenarios/${created.id}`);
}

// Edit a scenario's name/description. The edit form is its own route, so on success we
// refresh the detail and return to it.
export async function updateCostScenario(formData: FormData) {
  const id = formData.get("id") as string;
  const projectId = formData.get("projectId") as string;
  const payload = headerPayload(formData);
  if (!id || !payload.name) return;

  const res = await fetch(`${apiBaseUrl()}/api/cost-scenarios/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/projects/${projectId}/cost-scenarios/${id}`);
  redirect(`/projects/${projectId}/cost-scenarios/${id}`);
}

// Choose (or clear) the bid for one work package within a scenario. Bound to a per-row
// auto-submitting select on the detail page: a chosen bidId upserts the selection; an empty
// bidId excludes the work package. It stays on the page — revalidatePath refreshes the
// breakdown and totals in place.
export async function setScenarioSelection(formData: FormData) {
  const scenarioId = formData.get("scenarioId") as string;
  const projectId = formData.get("projectId") as string;
  const workPackageId = formData.get("workPackageId") as string;
  const bidId = (formData.get("bidId") as string)?.trim();
  if (!scenarioId || !workPackageId) return;

  const res = bidId
    ? await fetch(`${apiBaseUrl()}/api/cost-scenarios/${scenarioId}/selections`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ workPackageId, bidId }),
      })
    : await fetch(
        `${apiBaseUrl()}/api/cost-scenarios/${scenarioId}/work-packages/${workPackageId}`,
        { method: "DELETE" },
      );

  if (!res.ok && res.status !== 404) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  revalidatePath(`/projects/${projectId}/cost-scenarios/${scenarioId}`);
}

export async function deleteCostScenario(formData: FormData) {
  const id = formData.get("id") as string;
  const projectId = formData.get("projectId") as string;
  if (!id || !projectId) return;

  const res = await fetch(`${apiBaseUrl()}/api/cost-scenarios/${id}`, {
    method: "DELETE",
  });

  if (!res.ok && res.status !== 404) {
    throw new Error(await describeApiError(res, "common.actionError"));
  }

  // The detail page is gone; return to the project's scenario list.
  revalidatePath(`/projects/${projectId}/cost-scenarios`);
  redirect(`/projects/${projectId}/cost-scenarios`);
}
