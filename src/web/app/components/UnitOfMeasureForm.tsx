import {
  UNIT_CATEGORIES,
  UNIT_CATEGORY_LABELS,
  type UnitOfMeasure,
} from "@/app/lib/api";
import styles from "@/app/page.module.css";

interface UnitOfMeasureFormProps {
  // A server action that takes the submitted FormData (defineUnitOfMeasure / updateUnitOfMeasure).
  action: (formData: FormData) => void | Promise<void>;
  // When editing, the unit whose fields seed the form. Omit to render a blank "create" form.
  unit?: UnitOfMeasure;
  submitLabel: string;
}

/**
 * The create/edit form for a unit of measure. Nearly identical for both flows;
 * the differences are the bound server action, the hidden `id`, and the `code`
 * field — code is the canonical, immutable identifier, so it is editable only
 * when defining a new unit and shown read-only when editing. Aliases are entered
 * as a comma-separated list; the server action splits them and the backend
 * normalises each one.
 */
export function UnitOfMeasureForm({
  action,
  unit,
  submitLabel,
}: UnitOfMeasureFormProps) {
  const isEdit = Boolean(unit);

  return (
    <form action={action} className={styles.form}>
      {unit ? <input type="hidden" name="id" value={unit.id} /> : null}
      {isEdit ? (
        // Code can't change after definition; show it disabled (so it isn't
        // resubmitted) rather than as an editable field.
        <input
          name="code"
          placeholder="Code"
          defaultValue={unit?.code ?? ""}
          disabled
        />
      ) : (
        <input name="code" placeholder="Code (e.g. m, mp, buc)" required />
      )}
      <input
        name="name"
        placeholder="Name (e.g. metre, square metre)"
        defaultValue={unit?.name ?? ""}
        required
      />
      <select name="category" defaultValue={unit?.category ?? "Other"}>
        {UNIT_CATEGORIES.map((c) => (
          <option key={c} value={c}>
            {UNIT_CATEGORY_LABELS[c]}
          </option>
        ))}
      </select>
      <input
        name="aliases"
        placeholder="Aliases, comma-separated (optional)"
        defaultValue={unit?.aliases.join(", ") ?? ""}
      />
      <button type="submit">{submitLabel}</button>
    </form>
  );
}
