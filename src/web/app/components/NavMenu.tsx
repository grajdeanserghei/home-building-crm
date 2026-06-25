"use client";

import { useState } from "react";
import Link from "next/link";
import { t } from "@/app/lib/i18n";
import styles from "./Nav.module.css";

// The header's section links live here rather than in the server `Nav` so they can
// collapse behind a hamburger toggle on small screens. On desktop the toggle is
// hidden (CSS) and the links render inline as before; on phones/tablets the links
// become a panel that opens below the bar and closes again on navigation.
const LINKS = [
  { href: "/projects", key: "nav.projects" },
  { href: "/contractors", key: "nav.contractors" },
  { href: "/contracts", key: "nav.contracts" },
  { href: "/units-of-measure", key: "nav.unitsOfMeasure" },
  { href: "/trades", key: "nav.trades" },
] as const;

export function NavMenu() {
  const [open, setOpen] = useState(false);

  return (
    <>
      <button
        type="button"
        className={styles.menuToggle}
        aria-label={t("nav.menu")}
        aria-expanded={open}
        aria-controls="nav-links"
        onClick={() => setOpen((o) => !o)}
      >
        <svg
          width="22"
          height="22"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          aria-hidden="true"
        >
          {open ? (
            <>
              <line x1="6" y1="6" x2="18" y2="18" />
              <line x1="18" y1="6" x2="6" y2="18" />
            </>
          ) : (
            <>
              <line x1="3" y1="6" x2="21" y2="6" />
              <line x1="3" y1="12" x2="21" y2="12" />
              <line x1="3" y1="18" x2="21" y2="18" />
            </>
          )}
        </svg>
      </button>
      <div
        id="nav-links"
        className={`${styles.links} ${open ? styles.linksOpen : ""}`}
      >
        {LINKS.map((l) => (
          <Link
            key={l.href}
            href={l.href}
            className={styles.link}
            onClick={() => setOpen(false)}
          >
            {t(l.key)}
          </Link>
        ))}
      </div>
    </>
  );
}
