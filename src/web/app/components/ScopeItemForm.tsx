import {
  SCOPE_ITEM_REQUIREMENTS,
  SCOPE_ITEM_REQUIREMENT_LABELS,
  type ScopeItemRequirement,
} from "@/app/lib/api";
import styles from "@/app/page.module.css";

interface ScopeItemFormProps {
  // The server action taking the submitted FormData (addScopeItem).
  action: (formData: FormData) => void | Promise<void>;
  // The owning work package, carried as a hidden field for routing/revalidate.
  workPackageId: string;
  // Suggested next order (one past the existing scope items, 1-based).
  defaultSequence?: number;
}

/**
 * The add-a-scope-item form for a work package. A scope item is the owner's own up-front
 * sub-scope (e.g. within "Instalații termice": Încălzire pardoseală, Cameră tehnică gaz).
 * Its name must be unique within the package; Requirement marks whether it is mandatory or
 * could be dropped/deferred if the budget is tight. Defaults to Mandatory.
 */
export function ScopeItemForm({
  action,
  workPackageId,
  defaultSequence,
}: ScopeItemFormProps) {
  const defaultRequirement: ScopeItemRequirement = "Mandatory";

  return (
    <form action={action} className={styles.form}>
      <input type="hidden" name="workPackageId" value={workPackageId} />
      <input
        name="name"
        placeholder="Scope item name (e.g. Încălzire pardoseală)"
        required
      />
      <label className={styles.fieldLabel}>
        Requirement
        <select name="requirement" defaultValue={defaultRequirement}>
          {SCOPE_ITEM_REQUIREMENTS.map((r) => (
            <option key={r} value={r}>
              {SCOPE_ITEM_REQUIREMENT_LABELS[r]}
            </option>
          ))}
        </select>
      </label>
      <input
        name="sequence"
        type="number"
        min={1}
        step={1}
        placeholder="Order"
        defaultValue={defaultSequence ?? 1}
      />
      <input name="description" placeholder="Notes (optional)" />
      <button type="submit">Add scope item</button>
    </form>
  );
}
