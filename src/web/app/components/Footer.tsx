import { t } from "../lib/i18n";
import { appVersionLabel } from "../lib/version";
import styles from "./Footer.module.css";

// Persistent page footer rendered by the root layout, so the app version is visible
// from every page. A plain Server Component: the version is read from server-side env
// (see lib/version.ts) at render time, no client JS involved.
export function Footer() {
  return (
    <footer className={styles.footer}>
      <div className={styles.inner}>
        <span className={styles.brand}>{t("nav.brand")}</span>
        <span className={styles.version} title={t("footer.versionLabel")}>
          {appVersionLabel()}
        </span>
      </div>
    </footer>
  );
}
