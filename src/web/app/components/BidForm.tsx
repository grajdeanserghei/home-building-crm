import { type Bid, type Contractor } from "@/app/lib/api";
import styles from "@/app/page.module.css";

// Turn an ISO timestamp (or null) into the `yyyy-MM-dd` value an <input type="date">
// expects, so an existing bid's first-contact date pre-fills the editor.
function toDateInputValue(value?: string | null): string {
  if (!value) return "";
  return new Date(value).toISOString().slice(0, 10);
}

interface BidFormProps {
  // A server action that takes the submitted FormData (openBid / updateBid).
  action: (formData: FormData) => void | Promise<void>;
  // The owning work package, carried as a hidden field when opening a bid.
  workPackageId: string;
  // When editing, the bid whose fields seed the form. Omit to render a blank "open" form.
  bid?: Bid;
  // For an "open" form, the contractors that may still be picked (those without a bid
  // on this work package). Ignored when editing — a bid's contractor never changes.
  contractors?: Contractor[];
  submitLabel: string;
}

/**
 * The form for opening or editing a bid. Opening requires choosing a contractor; editing
 * only touches the free-text summary and first-contact date (the backend's update command
 * omits the contractor and the status, which has its own lifecycle endpoint).
 */
export function BidForm({
  action,
  workPackageId,
  bid,
  contractors,
  submitLabel,
}: BidFormProps) {
  const editing = Boolean(bid);

  return (
    <form action={action} className={styles.form}>
      {editing ? (
        <input type="hidden" name="id" value={bid!.id} />
      ) : (
        <input type="hidden" name="workPackageId" value={workPackageId} />
      )}

      {editing ? null : (
        <select name="contractorId" defaultValue="" required>
          <option value="" disabled>
            Select contractor…
          </option>
          {(contractors ?? []).map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      )}

      <label className={styles.fieldLabel}>
        First contacted
        <input
          name="firstContactedOn"
          type="date"
          defaultValue={toDateInputValue(bid?.firstContactedOn)}
        />
      </label>

      <input
        name="summary"
        placeholder="Summary (e.g. quoted 120k, slow to respond)"
        defaultValue={bid?.summary ?? ""}
      />

      <button type="submit">{submitLabel}</button>
    </form>
  );
}
