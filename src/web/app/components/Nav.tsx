import Link from "next/link";
import { setCurrentProject } from "../actions";
import { resolveCurrentProject } from "../lib/current-project";
import { t } from "../lib/i18n";
import type { Project } from "../lib/api";
import { ProjectSwitcher } from "./ProjectSwitcher";
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
        <div className={styles.links}>
          <Link href="/projects" className={styles.link}>
            {t("nav.projects")}
          </Link>
          <Link href="/contractors" className={styles.link}>
            {t("nav.contractors")}
          </Link>
          <Link href="/contracts" className={styles.link}>
            {t("nav.contracts")}
          </Link>
          <Link href="/units-of-measure" className={styles.link}>
            {t("nav.unitsOfMeasure")}
          </Link>
          <Link href="/trades" className={styles.link}>
            {t("nav.trades")}
          </Link>
        </div>
      </div>
    </nav>
  );
}
