"use client";

import { useState } from "react";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface ConfirmDeleteButtonProps {
  // The server action that performs the deletion once the user confirms.
  action: (formData: FormData) => void | Promise<void>;
  // Hidden form fields the action expects, e.g. { id, projectId }. Rendered as
  // <input type="hidden"> inside the confirm form so the action receives them unchanged.
  fields: Record<string, string>;
  // Dialog heading, e.g. "Ștergi proiectul?".
  title: string;
  // Dialog body template; its single {name} placeholder is rendered bold.
  bodyTemplate: string;
  // The thing being removed — substituted into bodyTemplate's {name} and shown bold.
  name: string;
  // Trigger button label. Defaults to "Șterge".
  triggerLabel?: string;
  // Trigger button style. Defaults to the destructive `delete` style; pass `chipRemove`
  // for a chip "×", etc.
  triggerClassName?: string;
  // Optional accessible label for icon-only triggers (e.g. the chip "×").
  triggerAriaLabel?: string;
  // Confirm button label inside the dialog. Defaults to "Șterge".
  confirmLabel?: string;
}

/**
 * Destructive-action button that gates its submit behind a confirmation dialog naming what
 * is about to be removed (UI principle #10 — "Every delete is confirmed before it happens").
 * The deletion itself stays a progressively-enhanced form bound to a server action; this
 * component only inserts the confirm step so an accidental click can't destroy data.
 *
 * Generic over every delete/remove on the site: pass the action, the hidden fields it
 * expects, and the strings to show. Trigger styling is overridable so the same component
 * serves a loud "Șterge" button, a quiet "Elimină", and a chip "×".
 */
export function ConfirmDeleteButton({
  action,
  fields,
  title,
  bodyTemplate,
  name,
  triggerLabel,
  triggerClassName,
  triggerAriaLabel,
  confirmLabel,
}: ConfirmDeleteButtonProps) {
  const [open, setOpen] = useState(false);
  const [before, after] = bodyTemplate.split("{name}");

  return (
    <>
      <button
        type="button"
        className={triggerClassName ?? styles.delete}
        aria-label={triggerAriaLabel}
        onClick={() => setOpen(true)}
      >
        {triggerLabel ?? t("common.delete")}
      </button>

      {open ? (
        <div
          className={styles.confirmOverlay}
          role="dialog"
          aria-modal="true"
          onClick={() => setOpen(false)}
        >
          <div
            className={styles.confirmDialog}
            onClick={(e) => e.stopPropagation()}
          >
            <h3 className={styles.confirmTitle}>{title}</h3>
            <p className={styles.confirmBody}>
              {before}
              <strong>{name}</strong>
              {after}
            </p>
            <div className={styles.confirmActions}>
              <button
                type="button"
                className={styles.edit}
                onClick={() => setOpen(false)}
              >
                {t("common.cancel")}
              </button>
              <form action={action}>
                {Object.entries(fields).map(([key, value]) => (
                  <input key={key} type="hidden" name={key} value={value} />
                ))}
                <button type="submit" className={styles.delete}>
                  {confirmLabel ?? t("common.delete")}
                </button>
              </form>
            </div>
          </div>
        </div>
      ) : null}
    </>
  );
}
