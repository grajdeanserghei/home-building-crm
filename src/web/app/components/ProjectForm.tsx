import {
  PROJECT_STATUSES,
  PROJECT_STATUS_LABELS,
  type Project,
} from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// Turn an ISO timestamp (or null) into the `yyyy-MM-dd` value an <input type="date">
// expects, so an existing project's due date pre-fills the editor.
function toDateInputValue(value?: string | null): string {
  if (!value) return "";
  return new Date(value).toISOString().slice(0, 10);
}

interface ProjectFormProps {
  // A server action that takes the submitted FormData (createProject / updateProject).
  action: (formData: FormData) => void | Promise<void>;
  // When editing, the project whose fields seed the form. Omit to render a blank "create" form.
  project?: Project;
  submitLabel: string;
}

/**
 * The create/edit form for a project. It is field-for-field identical for both flows;
 * only the bound server action and the presence of a hidden `id` differ.
 */
export function ProjectForm({ action, project, submitLabel }: ProjectFormProps) {
  return (
    <form action={action} className={styles.form}>
      {project ? <input type="hidden" name="id" value={project.id} /> : null}
      <input
        name="name"
        placeholder={t("projects.namePlaceholder")}
        defaultValue={project?.name ?? ""}
        required
      />
      <input
        name="description"
        placeholder={t("projects.descriptionPlaceholder")}
        defaultValue={project?.description ?? ""}
      />
      <select name="status" defaultValue={project?.status ?? "Planned"}>
        {PROJECT_STATUSES.map((s) => (
          <option key={s} value={s}>
            {PROJECT_STATUS_LABELS[s]}
          </option>
        ))}
      </select>
      <input
        name="dueDate"
        type="date"
        defaultValue={toDateInputValue(project?.dueDate)}
      />
      <label className={styles.fieldLabel}>
        {t("projects.apartmentUnits")}
        <input
          name="apartmentUnits"
          type="number"
          min={1}
          step={1}
          defaultValue={project?.apartmentUnits ?? 1}
        />
      </label>
      <button type="submit">{submitLabel}</button>
    </form>
  );
}
