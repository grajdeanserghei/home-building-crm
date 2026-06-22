import { type UnitOfMeasure } from "@/app/lib/api";
import styles from "@/app/page.module.css";

interface UnitOfMeasureActiveToggleProps {
  // The setUnitOfMeasureActive server action, passed from the server component.
  action: (formData: FormData) => void | Promise<void>;
  unit: UnitOfMeasure;
}

/**
 * Retire/restore control for a unit of measure. Deactivation is reversible (the
 * unit is hidden from use, not deleted) so — unlike the destructive delete
 * buttons elsewhere — it submits directly without a confirmation modal. The
 * hidden `isActive` carries the target state: the opposite of the current one.
 */
export function UnitOfMeasureActiveToggle({
  action,
  unit,
}: UnitOfMeasureActiveToggleProps) {
  return (
    <form action={action}>
      <input type="hidden" name="id" value={unit.id} />
      <input type="hidden" name="isActive" value={String(!unit.isActive)} />
      <button
        type="submit"
        className={unit.isActive ? styles.delete : styles.edit}
      >
        {unit.isActive ? "Deactivate" : "Activate"}
      </button>
    </form>
  );
}
