import { NOTE_TYPES, NOTE_TYPE_LABELS } from "@/app/lib/api";
import styles from "@/app/page.module.css";

interface BidNoteFormProps {
  // The logBidNote server action.
  action: (formData: FormData) => void | Promise<void>;
  // The bid the note is appended to, carried as a hidden field.
  bidId: string;
  // Today's date (yyyy-MM-dd), used to pre-fill the "occurred on" picker. Passed in so
  // the component stays a pure server component (the page provides the clock).
  today: string;
}

/**
 * The form for logging a dated interaction (meeting / call / email / note) on a bid's
 * discussion log. The author is filled in server-side from the current user.
 */
export function BidNoteForm({ action, bidId, today }: BidNoteFormProps) {
  return (
    <form action={action} className={styles.form}>
      <input type="hidden" name="bidId" value={bidId} />
      <select name="type" defaultValue="Note">
        {NOTE_TYPES.map((t) => (
          <option key={t} value={t}>
            {NOTE_TYPE_LABELS[t]}
          </option>
        ))}
      </select>
      <label className={styles.fieldLabel}>
        Occurred on
        <input name="occurredOn" type="date" defaultValue={today} required />
      </label>
      <input
        name="content"
        placeholder="What was discussed"
        required
      />
      <button type="submit">Log note</button>
    </form>
  );
}
