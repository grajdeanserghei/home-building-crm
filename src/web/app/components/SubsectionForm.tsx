import type { Subsection } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface SubsectionFormProps {
  // The server action taking the submitted FormData (addSubsection / updateSubsection).
  action: (formData: FormData) => void | Promise<void>;
  // The owning BoQ and section, carried as hidden fields for routing/revalidate.
  boqId: string;
  sectionId: string;
  // Suggested next order (one past the existing subsections, 1-based). Used only when adding.
  defaultSequence?: number;
  // When present, the form edits this existing subsection: fields are seeded with its values
  // and its id is carried as a hidden field for the PUT route.
  subsection?: Subsection;
  // Submit-button caption — defaults to the "add" label.
  submitLabel?: string;
}

/**
 * The subsection form for a section, used for both adding and editing — a fixed second
 * level of grouping (Excavation, Reinforcement, …) within a section. It inherits the BoQ's
 * pricing currency server-side.
 *
 * Pass `subsection` to edit an existing subsection — its values seed the inputs and its id
 * is submitted as a hidden field. Omit it to add a fresh subsection.
 */
export function SubsectionForm({
  action,
  boqId,
  sectionId,
  defaultSequence,
  subsection,
  submitLabel,
}: SubsectionFormProps) {
  return (
    <form action={action} className={styles.form}>
      <input type="hidden" name="boqId" value={boqId} />
      <input type="hidden" name="sectionId" value={sectionId} />
      {subsection ? (
        <input type="hidden" name="subsectionId" value={subsection.id} />
      ) : null}
      <input
        name="name"
        placeholder={t("subsections.namePlaceholder")}
        defaultValue={subsection?.name}
        required
      />
      <input
        name="sequence"
        type="number"
        min={1}
        step={1}
        placeholder={t("sections.orderPlaceholder")}
        defaultValue={subsection?.sequence ?? defaultSequence ?? 1}
      />
      <input
        name="description"
        placeholder={t("boq.notesPlaceholder")}
        defaultValue={subsection?.description ?? undefined}
      />
      <button type="submit">{submitLabel ?? t("subsections.add")}</button>
    </form>
  );
}
