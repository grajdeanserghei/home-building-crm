import {
  LineItemFields,
  type LineItemFieldsProps,
} from "@/app/components/LineItemFields";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface LineItemFormProps extends LineItemFieldsProps {
  // The server action taking the submitted FormData (addLineItem / reviseLineItem).
  action: (formData: FormData) => void | Promise<void>;
  // Submit-button caption — defaults to the "add" label.
  submitLabel?: string;
}

/**
 * The full-page line-item form, used by the standalone add/edit routes (and the no-JS
 * fallback for the overlay). It is a progressively-enhanced `<form>` bound to a server
 * action that redirects back to the detail page on success. The fields themselves live in
 * `LineItemFields`, shared with the modal variant so the two never drift apart.
 *
 * Pass `lineItem` to edit an existing row — its values seed the inputs and its id is
 * submitted as a hidden field. Omit it to add a fresh row.
 */
export function LineItemForm({
  action,
  submitLabel,
  ...fields
}: LineItemFormProps) {
  return (
    <form action={action} className={styles.form}>
      <LineItemFields {...fields} />
      <button type="submit">{submitLabel ?? t("lineItems.add")}</button>
    </form>
  );
}
