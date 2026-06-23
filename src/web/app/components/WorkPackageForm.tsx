import { type WorkPackage } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// Turn an ISO timestamp (or null) into the `yyyy-MM-dd` value an <input type="date">
// expects, so an existing package's planned dates pre-fill the editor.
function toDateInputValue(value?: string | null): string {
  if (!value) return "";
  return new Date(value).toISOString().slice(0, 10);
}

interface WorkPackageFormProps {
  // A server action that takes the submitted FormData (defineWorkPackage / updateWorkPackage).
  action: (formData: FormData) => void | Promise<void>;
  // The owning project, carried through as a hidden field for the action's revalidate/redirect.
  projectId: string;
  // When editing, the work package whose fields seed the form. Omit to render a blank "create" form.
  workPackage?: WorkPackage;
  // For a new package, the suggested next sequence (one past the existing packages, 1-based).
  defaultSequence?: number;
  submitLabel: string;
}

/**
 * The create/edit form for a work package. It is field-for-field identical for both flows;
 * only the bound server action and the presence of a hidden `id` differ. Status is not
 * shown: the backend forbids editing it (lifecycle transitions get dedicated endpoints).
 */
export function WorkPackageForm({
  action,
  projectId,
  workPackage,
  defaultSequence,
  submitLabel,
}: WorkPackageFormProps) {
  return (
    <form action={action} className={styles.form}>
      <input type="hidden" name="projectId" value={projectId} />
      {workPackage ? (
        <input type="hidden" name="id" value={workPackage.id} />
      ) : null}
      <input
        name="name"
        placeholder={t("workPackages.namePlaceholder")}
        defaultValue={workPackage?.name ?? ""}
        required
      />
      <input
        name="description"
        placeholder={t("workPackages.descriptionPlaceholder")}
        defaultValue={workPackage?.description ?? ""}
      />
      <input
        name="sequence"
        type="number"
        min={1}
        step={1}
        placeholder={t("workPackages.orderPlaceholder")}
        defaultValue={workPackage?.sequence ?? defaultSequence ?? 1}
      />
      <label className={styles.fieldLabel}>
        {t("workPackages.plannedStart")}
        <input
          name="plannedStartDate"
          type="date"
          defaultValue={toDateInputValue(workPackage?.plannedStartDate)}
        />
      </label>
      <label className={styles.fieldLabel}>
        {t("workPackages.plannedEnd")}
        <input
          name="plannedEndDate"
          type="date"
          defaultValue={toDateInputValue(workPackage?.plannedEndDate)}
        />
      </label>
      <button type="submit">{submitLabel}</button>
    </form>
  );
}
