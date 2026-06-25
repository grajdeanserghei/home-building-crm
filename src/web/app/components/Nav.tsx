import Link from "next/link";
import { setCurrentProject } from "../actions";
import { resolveCurrentProject } from "../lib/current-project";
import { t } from "../lib/i18n";
import type { Project } from "../lib/api";
import { ProjectSwitcher } from "./ProjectSwitcher";
import { NavMenu } from "./NavMenu";
import styles from "./Nav.module.css";

// Persistent top navigation rendered by the root layout, so the main sections are
// reachable from every page. The brand links to the project dashboard ("/"); the
// switcher beside it scopes the UI to a project, and "Proiecte" manages the list.
export async function Nav() {
  let projects: Project[] = [];
  let currentId: string | null = null;
  try {
    const resolved = await resolveCurrentProject();
    projects = resolved.projects;
    currentId = resolved.current?.id ?? null;
  } catch {
    // Keep the header usable even if the API is unreachable — the switcher just
    // renders nothing and the section links still work.
  }

  return (
    <nav className={styles.nav}>
      <div className={styles.inner}>
        <div className={styles.brandGroup}>
          <Link href="/" className={styles.brand}>
            {t("nav.brand")}
          </Link>
          <ProjectSwitcher
            projects={projects}
            currentId={currentId}
            action={setCurrentProject}
          />
        </div>
        <NavMenu />
      </div>
    </nav>
  );
}
