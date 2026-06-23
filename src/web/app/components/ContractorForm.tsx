import { type Contractor } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
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
        placeholder={t("contractors.companyName")}
        defaultValue={contractor?.name ?? ""}
        required
      />
      <input
        name="fiscalCode"
        placeholder={t("contractors.fiscalCodePlaceholder")}
        defaultValue={contractor?.fiscalCode ?? ""}
      />
      <input
        name="registrationNumber"
        placeholder={t("contractors.registrationNumberPlaceholder")}
        defaultValue={contractor?.registrationNumber ?? ""}
      />
      <input
        name="personName"
        placeholder={t("contractors.contactPersonPlaceholder")}
        defaultValue={contact?.personName ?? ""}
      />
      <input
        name="email"
        type="email"
        placeholder={t("contractors.emailPlaceholder")}
        defaultValue={contact?.email ?? ""}
      />
      <input
        name="phone"
        placeholder={t("contractors.phonePlaceholder")}
        defaultValue={contact?.phone ?? ""}
      />
      <input
        name="street"
        placeholder={t("contractors.streetPlaceholder")}
        defaultValue={address?.street ?? ""}
      />
      <input
        name="city"
        placeholder={t("contractors.cityPlaceholder")}
        defaultValue={address?.city ?? ""}
      />
      <input
        name="county"
        placeholder={t("contractors.countyPlaceholder")}
        defaultValue={address?.county ?? ""}
      />
      <input
        name="postalCode"
        placeholder={t("contractors.postalCodePlaceholder")}
        defaultValue={address?.postalCode ?? ""}
      />
      <input
        name="country"
        placeholder={t("contractors.countryPlaceholder")}
        defaultValue={address?.country ?? ""}
      />
      <input
        name="notes"
        placeholder={t("contractors.notesPlaceholder")}
        defaultValue={contractor?.notes ?? ""}
      />
      <button type="submit">{submitLabel}</button>
    </form>
  );
}
