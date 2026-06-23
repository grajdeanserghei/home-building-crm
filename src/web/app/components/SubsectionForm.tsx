import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface SubsectionFormProps {
  // The server action taking the submitted FormData (addSubsection).
  action: (formData: FormData) => void | Promise<void>;
  // The owning BoQ and section, carried as hidden fields for routing/revalidate.
  boqId: string;
  sectionId: string;
  // Suggested next order (one past the existing subsections, 1-based).
  defaultSequence?: number;
}

/**
 * The add-a-subsection form for a section — a fixed second level of grouping (Excavation,
 * Reinforcement, …) within a section. It inherits the BoQ's pricing currency server-side.
 */
export function SubsectionForm({
  action,
  boqId,
  sectionId,
  defaultSequence,
}: SubsectionFormProps) {
  return (
    <form action={action} className={styles.form}>
      <input type="hidden" name="boqId" value={boqId} />
      <input type="hidden" name="sectionId" value={sectionId} />
      <input
        name="name"
        placeholder={t("subsections.namePlaceholder")}
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
      <input name="description" placeholder={t("boq.notesPlaceholder")} />
      <button type="submit">{t("subsections.add")}</button>
    </form>
  );
}
