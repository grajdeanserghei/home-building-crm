import { apiBaseUrl, getBidBoq } from "@/app/lib/api";

// Proxies the BoQ Excel export from the .NET backend. The browser cannot reach API_BASE_URL
// directly (it is injected into the Next.js server only), so this route handler fetches the
// workbook server-side and streams it back, preserving the download headers. A separate route
// segment from the BoQ page (route.ts and page.tsx cannot share a segment in the App Router).
// The route is keyed by the bid id, so resolve the bid's single BoQ to get its export URL.
export async function GET(
  _request: Request,
  { params }: { params: Promise<{ id: string }> },
) {
  const { id } = await params;
  const boq = await getBidBoq(id);
  if (!boq) {
    return new Response(null, { status: 404 });
  }

  const upstream = await fetch(
    `${apiBaseUrl()}/api/bills-of-quantities/${boq.id}/export`,
    { cache: "no-store" },
  );

  if (!upstream.ok || !upstream.body) {
    return new Response(null, { status: upstream.status === 404 ? 404 : 502 });
  }

  // Pass through the content type and the attachment filename the backend chose.
  const headers = new Headers({ "cache-control": "no-store" });
  const contentType = upstream.headers.get("content-type");
  if (contentType) headers.set("content-type", contentType);
  const disposition = upstream.headers.get("content-disposition");
  if (disposition) headers.set("content-disposition", disposition);

  return new Response(upstream.body, { status: 200, headers });
}
