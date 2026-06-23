import styles from "@/app/page.module.css";

// A trade resolved to its display name, plus the id used by the add/remove forms.
interface NamedTrade {
  id: string;
  name: string;
}

interface TradeChipsProps {
  // The trades currently assigned to the owner (shown as removable chips).
  assigned: NamedTrade[];
  // The active trades not yet assigned (offered in the "add" dropdown).
  available: NamedTrade[];
  // The hidden field naming the owner the actions expect ("id" for a contractor,
  // "workPackageId" for a work package) and its value.
  ownerFieldName: string;
  ownerId: string;
  // Server actions: add posts {ownerFieldName, tradeId}; remove deletes the same.
  addAction: (formData: FormData) => void | Promise<void>;
  removeAction: (formData: FormData) => void | Promise<void>;
  // Pre-resolved labels (the component stays i18n-key-agnostic).
  emptyLabel: string;
  addLabel: string;
  selectPlaceholder: string;
  allAssignedLabel: string;
  removeAriaLabel: (name: string) => string;
}

/**
 * Manage a root's set of trades incrementally — each assigned trade is a chip whose ✕
 * submits a remove form, and an "add" dropdown lists the active, not-yet-assigned trades.
 * Mirrors how scope items are managed on the work-package detail page (immediate per-item
 * server actions), rather than a batched multi-select on the edit form.
 */
export function TradeChips({
  assigned,
  available,
  ownerFieldName,
  ownerId,
  addAction,
  removeAction,
  emptyLabel,
  addLabel,
  selectPlaceholder,
  allAssignedLabel,
  removeAriaLabel,
}: TradeChipsProps) {
  return (
    <>
      {assigned.length === 0 ? (
        <p className={styles.muted}>{emptyLabel}</p>
      ) : (
        <div className={styles.chipList}>
          {assigned.map((tr) => (
            <form key={tr.id} action={removeAction} className={styles.chip}>
              <input type="hidden" name={ownerFieldName} value={ownerId} />
              <input type="hidden" name="tradeId" value={tr.id} />
              <span>{tr.name}</span>
              <button
                type="submit"
                className={styles.chipRemove}
                aria-label={removeAriaLabel(tr.name)}
              >
                ×
              </button>
            </form>
          ))}
        </div>
      )}

      {available.length > 0 ? (
        <form action={addAction} className={styles.form}>
          <input type="hidden" name={ownerFieldName} value={ownerId} />
          <select name="tradeId" defaultValue="" required>
            <option value="" disabled>
              {selectPlaceholder}
            </option>
            {available.map((tr) => (
              <option key={tr.id} value={tr.id}>
                {tr.name}
              </option>
            ))}
          </select>
          <button type="submit">{addLabel}</button>
        </form>
      ) : (
        <p className={styles.muted}>{allAssignedLabel}</p>
      )}
    </>
  );
}
