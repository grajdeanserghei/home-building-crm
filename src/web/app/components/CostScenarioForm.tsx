import { t } from "@/app/lib/i18n";
import type { CostScenarioSummary } from "@/app/lib/api";
import styles from "@/app/page.module.css";

interface CostScenarioFormProps {
  // A server action that takes the submitted FormData (createCostScenario / updateCostScenario).
  action: (formData: FormData) => void | Promise<void>;
  // The owning project — always needed (carried as a hidden field for the create/redirect flow).
  projectId: string;
  // When editing, the scenario whose fields seed the form. Omit to render a blank "create" form.
  scenario?: Pick<CostScenarioSummary, "id" | "name"> & {
    description?: string | null;
  };
  submitLabel: string;
}

/**
 * The create/edit form for a cost scenario — just a name and an optional description. It is
 * field-for-field identical for both flows; only the bound server action and the presence of a
 * hidden `id` differ.
 */
export function CostScenarioForm({
  action,
  projectId,
  scenario,
  submitLabel,
}: CostScenarioFormProps) {
  return (
    <form action={action} className={styles.form}>
      <input type="hidden" name="projectId" value={projectId} />
      {scenario ? <input type="hidden" name="id" value={scenario.id} /> : null}
      <input
        name="name"
        placeholder={t("costScenario.namePlaceholder")}
        defaultValue={scenario?.name ?? ""}
        required
      />
      <input
        name="description"
        placeholder={t("costScenario.descriptionPlaceholder")}
        defaultValue={scenario?.description ?? ""}
      />
      <button type="submit">{submitLabel}</button>
    </form>
  );
}
