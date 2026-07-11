import Link from "next/link";
import { setCurrentProject, setDisplayCurrency } from "../actions";
import { resolveCurrentProject } from "../lib/current-project";
import { getDisplayCurrency } from "../lib/display-currency";
import { t } from "../lib/i18n";
import type { Project } from "../lib/api";
import { CurrencyToggle } from "./CurrencyToggle";
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

  // The global display-currency preference is a cookie read (no API call), so it stays available
  // even when the project list above failed to load.
  const displayCurrency = await getDisplayCurrency();

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
        <div className={styles.navEnd}>
          <CurrencyToggle current={displayCurrency} action={setDisplayCurrency} />
          <NavMenu />
        </div>
      </div>
    </nav>
  );
}
