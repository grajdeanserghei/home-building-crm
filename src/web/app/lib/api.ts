// Centralized access to the .NET backend.
//
// In Aspire, the AppHost injects the backend URL as the API_BASE_URL env var
// (see AppHost.cs -> WithEnvironment("API_BASE_URL", ...)). When running the
// Next.js app standalone (outside Aspire), it falls back to localhost.
export function apiBaseUrl(): string {
  return process.env.API_BASE_URL ?? "http://localhost:5000";
}

export type ProjectStatus =
  | "Planned"
  | "InProgress"
  | "OnHold"
  | "Completed";

// The status values in selection order, plus human-readable labels. Shared by the
// create/edit form and the projects table so they never drift apart.
export const PROJECT_STATUSES: readonly ProjectStatus[] = [
  "Planned",
  "InProgress",
  "OnHold",
  "Completed",
];

export const PROJECT_STATUS_LABELS: Record<ProjectStatus, string> = {
  Planned: "Planned",
  InProgress: "In Progress",
  OnHold: "On Hold",
  Completed: "Completed",
};

export interface Project {
  id: string;
  name: string;
  description?: string | null;
  status: ProjectStatus;
  createdAt: string;
  dueDate?: string | null;
}

export async function getProjects(): Promise<Project[]> {
  const res = await fetch(`${apiBaseUrl()}/api/projects`, { cache: "no-store" });
  if (!res.ok) {
    throw new Error(`Failed to load projects: ${res.status} ${res.statusText}`);
  }
  return res.json();
}

export async function getProject(id: string): Promise<Project | null> {
  const res = await fetch(`${apiBaseUrl()}/api/projects/${id}`, {
    cache: "no-store",
  });
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(`Failed to load project: ${res.status} ${res.statusText}`);
  }
  return res.json();
}

// Work packages ----------------------------------------------------------
//
// A work package is a defined scope of work within a project, procured as a unit
// (e.g. "La Roșu", "Tâmplărie"). The collection is nested under its project; an
// individual package is addressable by its own id. See docs/architecture/domain-model.md.

export type WorkPackageStatus =
  | "Defined"
  | "OpenForBids"
  | "Awarded"
  | "InProgress"
  | "Completed"
  | "Cancelled";

// The status values are surfaced read-only for now: the backend deliberately omits
// status from the edit command, since lifecycle transitions (award, etc.) carry
// invariants and get dedicated endpoints. Labels keep the table and any badge in sync.
export const WORK_PACKAGE_STATUS_LABELS: Record<WorkPackageStatus, string> = {
  Defined: "Defined",
  OpenForBids: "Open for Bids",
  Awarded: "Awarded",
  InProgress: "In Progress",
  Completed: "Completed",
  Cancelled: "Cancelled",
};

export interface WorkPackage {
  id: string;
  projectId: string;
  name: string;
  description?: string | null;
  status: WorkPackageStatus;
  sequence: number;
  plannedStartDate?: string | null;
  plannedEndDate?: string | null;
  awardedContractId?: string | null;
  createdAt: string;
}

export async function getWorkPackages(projectId: string): Promise<WorkPackage[]> {
  const res = await fetch(
    `${apiBaseUrl()}/api/projects/${projectId}/work-packages`,
    { cache: "no-store" },
  );
  if (!res.ok) {
    throw new Error(
      `Failed to load work packages: ${res.status} ${res.statusText}`,
    );
  }
  return res.json();
}

export async function getWorkPackage(id: string): Promise<WorkPackage | null> {
  const res = await fetch(`${apiBaseUrl()}/api/work-packages/${id}`, {
    cache: "no-store",
  });
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(
      `Failed to load work package: ${res.status} ${res.statusText}`,
    );
  }
  return res.json();
}
