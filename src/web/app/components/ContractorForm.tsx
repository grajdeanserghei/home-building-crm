import { type Contractor } from "@/app/lib/api";
import styles from "@/app/page.module.css";

interface ContractorFormProps {
  // A server action that takes the submitted FormData (registerContractor / updateContractor).
  action: (formData: FormData) => void | Promise<void>;
  // When editing, the contractor whose fields seed the form. Omit to render a blank "create" form.
  contractor?: Contractor;
  submitLabel: string;
}

/**
 * The create/edit form for a contractor. It is field-for-field identical for both
 * flows; only the bound server action and the presence of a hidden `id` differ.
 * Contact and address are flat inputs here; the server action nests them into the
 * value objects the backend expects.
 */
export function ContractorForm({
  action,
  contractor,
  submitLabel,
}: ContractorFormProps) {
  const contact = contractor?.contact;
  const address = contractor?.address;

  return (
    <form action={action} className={styles.form}>
      {contractor ? (
        <input type="hidden" name="id" value={contractor.id} />
      ) : null}
      <input
        name="name"
        placeholder="Company name"
        defaultValue={contractor?.name ?? ""}
        required
      />
      <input
        name="fiscalCode"
        placeholder="Fiscal code / CUI (optional)"
        defaultValue={contractor?.fiscalCode ?? ""}
      />
      <input
        name="registrationNumber"
        placeholder="Trade register no. / J (optional)"
        defaultValue={contractor?.registrationNumber ?? ""}
      />
      <input
        name="personName"
        placeholder="Contact person (optional)"
        defaultValue={contact?.personName ?? ""}
      />
      <input
        name="email"
        type="email"
        placeholder="Email (optional)"
        defaultValue={contact?.email ?? ""}
      />
      <input
        name="phone"
        placeholder="Phone (optional)"
        defaultValue={contact?.phone ?? ""}
      />
      <input
        name="street"
        placeholder="Street (optional)"
        defaultValue={address?.street ?? ""}
      />
      <input
        name="city"
        placeholder="City (optional)"
        defaultValue={address?.city ?? ""}
      />
      <input
        name="county"
        placeholder="County / județ (optional)"
        defaultValue={address?.county ?? ""}
      />
      <input
        name="postalCode"
        placeholder="Postal code (optional)"
        defaultValue={address?.postalCode ?? ""}
      />
      <input
        name="country"
        placeholder="Country (optional)"
        defaultValue={address?.country ?? ""}
      />
      <input
        name="notes"
        placeholder="Notes (optional)"
        defaultValue={contractor?.notes ?? ""}
      />
      <button type="submit">{submitLabel}</button>
    </form>
  );
}
