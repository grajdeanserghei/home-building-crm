"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import {
  LineItemFields,
  type LineItemFieldsProps,
} from "@/app/components/LineItemFields";
import type { LineItemFormResult } from "@/app/bills-of-quantities/actions";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface ModalLineItemFormProps extends LineItemFieldsProps {
  // A non-redirecting modal action that reports whether the save succeeded.
  action: (formData: FormData) => Promise<LineItemFormResult>;
  // Submit-button caption — defaults to the "add" label.
  submitLabel?: string;
}

/**
 * The line-item form as it appears inside the overlay. Unlike `LineItemForm`, the action
 * here returns a result rather than redirecting: on success we `router.back()` to dismiss the
 * overlay (the detail page underneath keeps its scroll), and on failure we surface the message
 * inline without leaving the form. The inputs are the shared `LineItemFields`.
 */
export function ModalLineItemForm({
  action,
  submitLabel,
  ...fields
}: ModalLineItemFormProps) {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  const onSubmit = (formData: FormData) => {
    setError(null);
    startTransition(async () => {
      const result = await action(formData);
      if (result.ok) {
        router.back();
      } else {
        setError(result.error);
      }
    });
  };

  return (
    <form action={onSubmit} className={styles.form}>
      <LineItemFields {...fields} />
      {error ? <p className={styles.error}>{error}</p> : null}
      <div className={styles.modalActions}>
        <button
          type="button"
          className={styles.edit}
          onClick={() => router.back()}
          disabled={pending}
        >
          {t("common.cancel")}
        </button>
        <button type="submit" disabled={pending}>
          {pending ? t("common.saving") : submitLabel ?? t("lineItems.add")}
        </button>
      </div>
    </form>
  );
}
