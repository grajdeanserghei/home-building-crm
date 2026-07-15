import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import { Nav } from "./components/Nav";
import { Footer } from "./components/Footer";
import { t } from "./lib/i18n";
import "./globals.css";

// "latin-ext" covers the Romanian diacritics (ă â î ș ț) used throughout the UI.
const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin", "latin-ext"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin", "latin-ext"],
});

export const metadata: Metadata = {
  title: t("meta.title"),
  description: t("meta.description"),
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="ro" className={`${geistSans.variable} ${geistMono.variable}`}>
      <body suppressHydrationWarning>
        <Nav />
        {children}
        <Footer />
      </body>
    </html>
  );
}
