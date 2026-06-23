import { cookies } from "next/headers";
import { getProjects, type Project } from "./api";

// Name of the cookie holding the id of the project the UI is currently scoped to.
// Read on the server in the layout (for the header switcher) and on the home page
// (for the dashboard); written by the `setCurrentProject` server action.
export const CURRENT_PROJECT_COOKIE = "currentProjectId";

/**
 * Resolve the project the UI is currently scoped to for this request.
 *
 * Fetches the full project list (also used to populate the header switcher) and
 * picks the selected one from the `currentProjectId` cookie. When the cookie is
 * missing — or points at a project that no longer exists — it falls back to the
 * first project so the dashboard is never empty while any project exists.
 *
 * Returns the list alongside the resolved project so callers fetch projects once.
 */
export async function resolveCurrentProject(): Promise<{
  projects: Project[];
  current: Project | null;
}> {
  const projects = await getProjects();
  const cookieStore = await cookies();
  const selectedId = cookieStore.get(CURRENT_PROJECT_COOKIE)?.value;
  const current = projects.find((p) => p.id === selectedId) ?? projects[0] ?? null;
  return { projects, current };
}
