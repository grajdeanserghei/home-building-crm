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
