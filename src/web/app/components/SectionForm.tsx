import type { Section } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface SectionFormProps {
  // The server action taking the submitted FormData (addSection / updateSection).
  action: (formData: FormData) => void | Promise<void>;
  // The owning BoQ, carried as a hidden field for routing/revalidate.
  boqId: string;
  // Suggested next order (one past the existing sections, 1-based). Used only when adding.
  defaultSequence?: number;
  // When present, the form edits this existing section: fields are seeded with its values
  // and its id is carried as a hidden field for the PUT route.
  section?: Section;
  // Submit-button caption — defaults to the "add" label.
  submitLabel?: string;
}

/**
 * The section form for a BoQ, used for both adding and editing. A section is the
 * contractor's own grouping of line items (Foundation, Roof, …); it inherits the BoQ's
 * pricing currency server-side.
 *
 * Pass `section` to edit an existing section — its values seed the inputs and its id is
 * submitted as a hidden field. Omit it to add a fresh section.
 */
export function SectionForm({
  action,
  boqId,
  defaultSequence,
  section,
  submitLabel,
}: SectionFormProps) {
  return (
    <form action={action} className={styles.form}>
      <input type="hidden" name="boqId" value={boqId} />
      {section ? (
        <input type="hidden" name="sectionId" value={section.id} />
      ) : null}
      <input
        name="name"
        placeholder={t("sections.namePlaceholder")}
        defaultValue={section?.name}
        required
      />
      <input
        name="sequence"
        type="number"
        min={1}
        step={1}
        placeholder={t("sections.orderPlaceholder")}
        defaultValue={section?.sequence ?? defaultSequence ?? 1}
      />
      <input
        name="description"
        placeholder={t("boq.notesPlaceholder")}
        defaultValue={section?.description ?? undefined}
      />
      <button type="submit">{submitLabel ?? t("sections.add")}</button>
    </form>
  );
}
