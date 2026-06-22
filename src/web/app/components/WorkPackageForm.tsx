import { type WorkPackage } from "@/app/lib/api";
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
  // For a new package, the suggested next sequence (count of existing packages).
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
        placeholder="Work package name (e.g. La Roșu)"
        defaultValue={workPackage?.name ?? ""}
        required
      />
      <input
        name="description"
        placeholder="Scope notes (optional)"
        defaultValue={workPackage?.description ?? ""}
      />
      <input
        name="sequence"
        type="number"
        min={0}
        step={1}
        placeholder="Order"
        defaultValue={workPackage?.sequence ?? defaultSequence ?? 0}
      />
      <label className={styles.fieldLabel}>
        Planned start
        <input
          name="plannedStartDate"
          type="date"
          defaultValue={toDateInputValue(workPackage?.plannedStartDate)}
        />
      </label>
      <label className={styles.fieldLabel}>
        Planned end
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
