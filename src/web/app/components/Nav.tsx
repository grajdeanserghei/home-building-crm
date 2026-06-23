import Link from "next/link";
import { t } from "../lib/i18n";
import styles from "./Nav.module.css";

// Persistent top navigation rendered by the root layout, so the main sections are
// reachable from every page (the home page is the projects list).
export function Nav() {
  return (
    <nav className={styles.nav}>
      <div className={styles.inner}>
        <Link href="/" className={styles.brand}>
          {t("nav.brand")}
        </Link>
        <div className={styles.links}>
          <Link href="/" className={styles.link}>
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
        </div>
      </div>
    </nav>
  );
}
