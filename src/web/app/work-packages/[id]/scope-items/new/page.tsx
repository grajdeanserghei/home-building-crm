import Link from "next/link";
import { notFound } from "next/navigation";
import { ScopeItemForm } from "@/app/components/ScopeItemForm";
import { addScopeItem } from "@/app/work-packages/actions";
import { getWorkPackage } from "@/app/lib/api";
import { t } from "@/app/lib/i18n";
import styles from "@/app/page.module.css";

// Adding a scope item (the owner's own up-front sub-scope) is a step away on its own route.
// The form's success action revalidates and returns to the package's read view.
export default async function NewScopeItemPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const workPackage = await getWorkPackage(id);

  if (!workPackage) {
    notFound();
  }

  return (
    <main className={styles.main}>
      <Link href={`/work-packages/${workPackage.id}`} className={styles.backLink}>
        {t("workPackages.backToWorkPackage")}
      </Link>
      <h1>{t("scopeItems.add")}</h1>
      <p className={styles.subtitle}>{t("scopeItems.subtitle")}</p>

      <section className={styles.card}>
        <ScopeItemForm
          action={addScopeItem}
          workPackageId={workPackage.id}
          defaultSequence={workPackage.scopeItems.length + 1}
        />
        <Link
          href={`/work-packages/${workPackage.id}`}
          className={styles.backLink}
        >
          {t("common.cancel")}
        </Link>
      </section>
    </main>
  );
}
