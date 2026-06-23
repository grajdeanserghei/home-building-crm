import Link from "next/link";
import { createProject } from "@/app/actions";
import { ProjectForm } from "@/app/components/ProjectForm";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// Creating a project is a rare action, so it lives on its own route rather than
// cluttering the home dashboard. On success the action redirects back to the list.
export default function NewProjectPage() {
  return (
    <main className={styles.main}>
      <h1>{t("projects.new")}</h1>
      <p className={styles.subtitle}>{t("projects.createSubtitle")}</p>

      <section className={styles.card}>
        <ProjectForm action={createProject} submitLabel={t("projects.add")} />
        <Link href="/" className={styles.backLink}>
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
