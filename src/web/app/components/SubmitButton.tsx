"use client";

import { useFormStatus } from "react-dom";
import { t } from "@/app/lib/i18n";

interface SubmitButtonProps {
  // Button label in the idle state.
  label: string;
  // Label while the form's action is running. Defaults to the generic "Se salvează…".
  pendingLabel?: string;
  // Optional class (e.g. styles.edit) so it matches the surrounding buttons.
  className?: string;
}

/**
 * A submit button that reflects its form's pending state: while the bound server action runs
 * (and, for actions that redirect, until navigation completes) it disables itself and swaps in a
 * pending label, so a click that triggers a server round-trip gives immediate visual feedback.
 *
 * Must be rendered inside a <form action={...}> — `useFormStatus` reads that form's status.
 */
export function SubmitButton({ label, pendingLabel, className }: SubmitButtonProps) {
  const { pending } = useFormStatus();

  return (
    <button
      type="submit"
      className={className}
      disabled={pending}
      aria-busy={pending}
    >
      {pending ? (pendingLabel ?? t("common.saving")) : label}
    </button>
  );
}
