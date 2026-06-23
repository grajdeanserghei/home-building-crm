"use client";

import { useRef } from "react";
import { t } from "@/app/lib/i18n";
import type { Project } from "@/app/lib/api";
import styles from "./Nav.module.css";

interface ProjectSwitcherProps {
  projects: Project[];
  currentId: string | null;
  // The setCurrentProject server action, passed down from the server Nav.
  action: (formData: FormData) => void | Promise<void>;
}

/**
 * Header dropdown that scopes the whole UI to a project.
 *
 * Changing the selection submits a progressively-enhanced form bound to the
 * `setCurrentProject` server action, which persists the choice in a cookie and
 * redirects to that project's dashboard. Rendered as a client component only so
 * the native `<select>` can auto-submit on change.
 */
export function ProjectSwitcher({
  projects,
  currentId,
  action,
}: ProjectSwitcherProps) {
  const formRef = useRef<HTMLFormElement>(null);

  if (projects.length === 0) {
    return null;
  }

  return (
    <form action={action} ref={formRef} className={styles.switcher}>
      <label htmlFor="project-switcher" className={styles.switcherLabel}>
        {t("nav.project")}
      </label>
      <select
        id="project-switcher"
        name="projectId"
        className={styles.switcherSelect}
        defaultValue={currentId ?? ""}
        onChange={() => formRef.current?.requestSubmit()}
      >
        {projects.map((p) => (
          <option key={p.id} value={p.id}>
            {p.name}
          </option>
        ))}
      </select>
    </form>
  );
}
