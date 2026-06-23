import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface SectionFormProps {
  // The server action taking the submitted FormData (addSection).
  action: (formData: FormData) => void | Promise<void>;
  // The owning BoQ, carried as a hidden field for routing/revalidate.
  boqId: string;
  // Suggested next order (one past the existing sections, 1-based).
  defaultSequence?: number;
}

/**
 * The add-a-section form for a BoQ. A section is the contractor's own grouping of line
 * items (Foundation, Roof, …); it inherits the BoQ's pricing currency server-side.
 */
export function SectionForm({
  action,
  boqId,
  defaultSequence,
}: SectionFormProps) {
  return (
    <form action={action} className={styles.form}>
      <input type="hidden" name="boqId" value={boqId} />
      <input
        name="name"
        placeholder={t("sections.namePlaceholder")}
        required
      />
      <input
        name="sequence"
        type="number"
        min={1}
        step={1}
        placeholder={t("sections.orderPlaceholder")}
        defaultValue={defaultSequence ?? 1}
      />
      <input
        name="description"
        placeholder={t("boq.notesPlaceholder")}
      />
      <button type="submit">{t("sections.add")}</button>
    </form>
  );
}
