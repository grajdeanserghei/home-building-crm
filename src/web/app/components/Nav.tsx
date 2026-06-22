import Link from "next/link";
import styles from "./Nav.module.css";

// Persistent top navigation rendered by the root layout, so the main sections are
// reachable from every page (the home page is the projects list).
export function Nav() {
  return (
    <nav className={styles.nav}>
      <div className={styles.inner}>
        <Link href="/" className={styles.brand}>
          Home Project Management
        </Link>
        <div className={styles.links}>
          <Link href="/" className={styles.link}>
            Projects
          </Link>
          <Link href="/contractors" className={styles.link}>
            Contractors
          </Link>
          <Link href="/contracts" className={styles.link}>
            Contracts
          </Link>
          <Link href="/units-of-measure" className={styles.link}>
            Units of measure
          </Link>
        </div>
      </div>
    </nav>
  );
}
