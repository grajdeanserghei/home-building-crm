import styles from "@/app/page.module.css";

// A right-pointing chevron that rotates down (via .boqChevronOpen) when its accordion item is
// expanded. Inline SVG with stroke="currentColor", matching the NavMenu toggle idiom. Shared by the
// read-view (BoqSections) and Arrange-mode (BoqDndBoard) disclosures.
export function BoqChevron({ open }: { open: boolean }) {
  return (
    <svg
      className={`${styles.boqChevron}${open ? ` ${styles.boqChevronOpen}` : ""}`}
      width="16"
      height="16"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2.5"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <polyline points="9 6 15 12 9 18" />
    </svg>
  );
}
