"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

interface ModalProps {
  // Heading shown in the dialog header.
  title: string;
  // Optional context line under the title (e.g. the owning section).
  subtitle?: string;
  children: React.ReactNode;
}

/**
 * Overlay shell for the intercepting-route form modals. It is reached only by client-side
 * navigation (an intercepted `<Link>`); a direct visit or refresh renders the real full page
 * instead. Closing — via the × button, the backdrop, or Escape — is `router.back()`, which
 * pops the intercepted URL and returns to the detail page with its scroll position intact.
 */
export function Modal({ title, subtitle, children }: ModalProps) {
  const router = useRouter();
  const close = () => router.back();

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") router.back();
    };
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [router]);

  return (
    <div
      className={styles.modalOverlay}
      role="dialog"
      aria-modal="true"
      onClick={close}
    >
      <div className={styles.modalDialog} onClick={(e) => e.stopPropagation()}>
        <div className={styles.modalHeader}>
          <div>
            <h2 className={styles.modalTitle}>{title}</h2>
            {subtitle ? (
              <p className={styles.modalSubtitle}>{subtitle}</p>
            ) : null}
          </div>
          <button
            type="button"
            className={styles.modalClose}
            onClick={close}
            aria-label={t("common.close")}
          >
            ×
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}
