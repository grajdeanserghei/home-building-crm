"use client";

import { useState } from "react";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface DeleteContractorButtonProps {
  // The deleteContractor server action, passed down from the server component.
  action: (formData: FormData) => void | Promise<void>;
  contractorId: string;
  contractorName: string;
}

/**
 * Delete control that asks for confirmation before submitting.
 *
 * The actual deletion is still a progressively-enhanced form bound to the
 * `deleteContractor` server action; this component only gates the submit behind a
 * confirmation overlay so an accidental click can't destroy a contractor.
 */
export function DeleteContractorButton({
  action,
  contractorId,
  contractorName,
}: DeleteContractorButtonProps) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <button
        type="button"
        className={styles.delete}
        onClick={() => setOpen(true)}
      >
        {t("common.delete")}
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
            <h3 className={styles.confirmTitle}>
              {t("contractors.deleteTitle")}
            </h3>
            <p className={styles.confirmBody}>
              {t("contractors.deleteBodyBefore")}{" "}
              <strong>{contractorName}</strong>
              {t("contractors.deleteBodyAfter")}
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
                <input type="hidden" name="id" value={contractorId} />
                <button type="submit" className={styles.delete}>
                  {t("common.delete")}
                </button>
              </form>
            </div>
          </div>
        </div>
      ) : null}
    </>
  );
}
