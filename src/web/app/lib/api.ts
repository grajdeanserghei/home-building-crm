// Centralized access to the .NET backend.
//
// In Aspire, the AppHost injects the backend URL as the API_BASE_URL env var
// (see AppHost.cs -> WithEnvironment("API_BASE_URL", ...)). When running the
// Next.js app standalone (outside Aspire), it falls back to localhost.
import { t } from "./i18n";

// Re-exported so existing `import { formatMoney } from "../lib/api"` call sites keep working;
// the implementation now lives in the shared ro-RO formatting module (./format).
export { formatMoney } from "./format";

export function apiBaseUrl(): string {
  return process.env.API_BASE_URL ?? "http://localhost:5000";
}

export type ProjectStatus =
  | "Planned"
  | "InProgress"
  | "OnHold"
  | "Completed";

// The status values in selection order, plus human-readable labels. Shared by the
// create/edit form and the projects table so they never drift apart.
export const PROJECT_STATUSES: readonly ProjectStatus[] = [
  "Planned",
  "InProgress",
  "OnHold",
  "Completed",
];

export const PROJECT_STATUS_LABELS: Record<ProjectStatus, string> = {
  Planned: t("enum.projectStatus.Planned"),
  InProgress: t("enum.projectStatus.InProgress"),
  OnHold: t("enum.projectStatus.OnHold"),
  Completed: t("enum.projectStatus.Completed"),
};

export interface Project {
  id: string;
  name: string;
  description?: string | null;
  status: ProjectStatus;
  createdAt: string;
  dueDate?: string | null;
}

export async function getProjects(): Promise<Project[]> {
  const res = await fetch(`${apiBaseUrl()}/api/projects`, { cache: "no-store" });
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`);
  }
  return res.json();
}

export async function getProject(id: string): Promise<Project | null> {
  const res = await fetch(`${apiBaseUrl()}/api/projects/${id}`, {
    cache: "no-store",
  });
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`);
  }
  return res.json();
}

// Work packages ----------------------------------------------------------
//
// A work package is a defined scope of work within a project, procured as a unit
// (e.g. "La Roșu", "Tâmplărie"). The collection is nested under its project; an
// individual package is addressable by its own id. See docs/architecture/domain-model.md.

export type WorkPackageStatus =
  | "Defined"
  | "OpenForBids"
  | "Awarded"
  | "InProgress"
  | "Completed"
  | "Cancelled";

// The statuses in lifecycle order, plus human-readable labels. Shared by the status
// control on the work-package page and the table badges so they never drift apart. Status
// is omitted from the edit command — lifecycle transitions go through a dedicated endpoint
// (see changeWorkPackageStatus). Awarded is reached only via the award flow, not the control.
export const WORK_PACKAGE_STATUSES: readonly WorkPackageStatus[] = [
  "Defined",
  "OpenForBids",
  "Awarded",
  "InProgress",
  "Completed",
  "Cancelled",
];

export const WORK_PACKAGE_STATUS_LABELS: Record<WorkPackageStatus, string> = {
  Defined: t("enum.workPackageStatus.Defined"),
  OpenForBids: t("enum.workPackageStatus.OpenForBids"),
  Awarded: t("enum.workPackageStatus.Awarded"),
  InProgress: t("enum.workPackageStatus.InProgress"),
  Completed: t("enum.workPackageStatus.Completed"),
  Cancelled: t("enum.workPackageStatus.Cancelled"),
};

// Whether a scope item is mandatory or could be dropped/deferred if the budget is tight.
// Mirrors the ScopeItemRequirement enum (persisted/serialized as its string name).
export type ScopeItemRequirement = "Mandatory" | "Optional";

export const SCOPE_ITEM_REQUIREMENTS: readonly ScopeItemRequirement[] = [
  "Mandatory",
  "Optional",
];

export const SCOPE_ITEM_REQUIREMENT_LABELS: Record<ScopeItemRequirement, string> = {
  Mandatory: t("enum.scopeItemRequirement.Mandatory"),
  Optional: t("enum.scopeItemRequirement.Optional"),
};

// An owner-defined sub-scope of a work package (e.g. within "Instalații termice":
// Încălzire pardoseală, Cameră tehnică gaz). Names are unique within the package. This is
// the owner's own up-front scoping — distinct from a contractor's BoQ section.
export interface ScopeItem {
  id: string;
  name: string;
  description?: string | null;
  requirement: ScopeItemRequirement;
  sequence: number;
}

export interface WorkPackage {
  id: string;
  projectId: string;
  name: string;
  description?: string | null;
  status: WorkPackageStatus;
  sequence: number;
  plannedStartDate?: string | null;
  plannedEndDate?: string | null;
  awardedContractId?: string | null;
  scopeItems: ScopeItem[];
  requiredTradeIds: string[]; // the trades this package requires (by id; the shared Trade vocabulary)
  createdAt: string;
}

export async function getWorkPackages(projectId: string): Promise<WorkPackage[]> {
  const res = await fetch(
    `${apiBaseUrl()}/api/projects/${projectId}/work-packages`,
    { cache: "no-store" },
  );
  if (!res.ok) {
    throw new Error(
      `${res.status} ${res.statusText}`,
    );
  }
  return res.json();
}

export async function getWorkPackage(id: string): Promise<WorkPackage | null> {
  const res = await fetch(`${apiBaseUrl()}/api/work-packages/${id}`, {
    cache: "no-store",
  });
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(
      `${res.status} ${res.statusText}`,
    );
  }
  return res.json();
}

// Contractors ------------------------------------------------------------
//
// A contractor is a firm that bids on and carries out work packages. It is global
// master data (not nested under a project); bids reference it by id. The contact
// and address are optional owned value objects, sent over the wire as nested
// objects (the backend collapses an all-empty one to null).
// See docs/architecture/domain-model.md.

export interface ContactInfo {
  personName?: string | null;
  email?: string | null;
  phone?: string | null;
}

export interface Address {
  street?: string | null;
  city?: string | null;
  county?: string | null;
  postalCode?: string | null;
  country?: string | null;
}

export interface Contractor {
  id: string;
  name: string;
  fiscalCode?: string | null;
  registrationNumber?: string | null;
  contact?: ContactInfo | null;
  address?: Address | null;
  notes?: string | null;
  tradeIds: string[]; // the trades this firm performs (by id; the shared Trade vocabulary)
  createdAt: string;
}

export async function getContractors(): Promise<Contractor[]> {
  const res = await fetch(`${apiBaseUrl()}/api/contractors`, {
    cache: "no-store",
  });
  if (!res.ok) {
    throw new Error(
      `${res.status} ${res.statusText}`,
    );
  }
  return res.json();
}

export async function getContractor(id: string): Promise<Contractor | null> {
  const res = await fetch(`${apiBaseUrl()}/api/contractors/${id}`, {
    cache: "no-store",
  });
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(
      `${res.status} ${res.statusText}`,
    );
  }
  return res.json();
}

// Units of measure -------------------------------------------------------
//
// A unit of measure is global master data used to quantify bill-of-quantities
// lines (e.g. "m" / metre, "buc" / piece). It carries a canonical, immutable
// `code`, a category, and optional aliases other tokens can be recognised by.
// It is never deleted — it is retired by deactivating it. See
// docs/architecture/domain-model.md.

export type UnitCategory =
  | "Length"
  | "Area"
  | "Volume"
  | "Mass"
  | "Count"
  | "Time"
  | "Other";

// The categories in selection order, plus human-readable labels. Shared by the
// create/edit form and the table so they never drift apart.
export const UNIT_CATEGORIES: readonly UnitCategory[] = [
  "Length",
  "Area",
  "Volume",
  "Mass",
  "Count",
  "Time",
  "Other",
];

export const UNIT_CATEGORY_LABELS: Record<UnitCategory, string> = {
  Length: t("enum.unitCategory.Length"),
  Area: t("enum.unitCategory.Area"),
  Volume: t("enum.unitCategory.Volume"),
  Mass: t("enum.unitCategory.Mass"),
  Count: t("enum.unitCategory.Count"),
  Time: t("enum.unitCategory.Time"),
  Other: t("enum.unitCategory.Other"),
};

export interface UnitOfMeasure {
  id: string;
  code: string;
  name: string;
  category: UnitCategory;
  aliases: string[];
  isActive: boolean;
  createdAt: string;
}

export async function getUnitsOfMeasure(
  includeInactive = true,
): Promise<UnitOfMeasure[]> {
  const res = await fetch(
    `${apiBaseUrl()}/api/units-of-measure?includeInactive=${includeInactive}`,
    { cache: "no-store" },
  );
  if (!res.ok) {
    throw new Error(
      `${res.status} ${res.statusText}`,
    );
  }
  return res.json();
}

export async function getUnitOfMeasure(
  id: string,
): Promise<UnitOfMeasure | null> {
  const res = await fetch(`${apiBaseUrl()}/api/units-of-measure/${id}`, {
    cache: "no-store",
  });
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(
      `${res.status} ${res.statusText}`,
    );
  }
  return res.json();
}

// Trades -----------------------------------------------------------------
//
// A trade is a category of specialized construction work (e.g. Zidărie, Instalații
// Electrice). It is a controlled, project-independent reference vocabulary: a
// contractor is tagged with the trades it performs and a work package with the trades
// it requires (both by id). Like a unit of measure it carries a unique canonical
// `name`, an optional short `code`, and is never deleted — it is retired by
// deactivating it. See docs/architecture/domain-model.md.

export interface Trade {
  id: string;
  name: string;
  code?: string | null;
  isActive: boolean;
  createdAt: string;
}

export async function getTrades(includeInactive = true): Promise<Trade[]> {
  const res = await fetch(
    `${apiBaseUrl()}/api/trades?includeInactive=${includeInactive}`,
    { cache: "no-store" },
  );
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`);
  }
  return res.json();
}

export async function getTrade(id: string): Promise<Trade | null> {
  const res = await fetch(`${apiBaseUrl()}/api/trades/${id}`, {
    cache: "no-store",
  });
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`);
  }
  return res.json();
}

// Bids -------------------------------------------------------------------
//
// A bid is one contractor's standing on one work package — the unit through which
// the procurement conversation is tracked, from first contact to selection. The
// collection is nested under its work package (the competing bids); an individual
// bid is an aggregate root addressable by its own id, and carries an internal
// discussion log of dated notes. There is at most one bid per (work package,
// contractor) pair, and at most one Selected bid per work package — selecting a
// winner rejects its rivals server-side. See docs/architecture/domain-model.md.

export type BidStatus =
  | "InDiscussion"
  | "BoqExpected"
  | "BoqReceived"
  | "Shortlisted"
  | "Selected"
  | "Rejected"
  | "Withdrawn";

// The statuses in lifecycle order, plus human-readable labels. Shared by the status
// control and the bids table so they never drift apart. A bid is opened InDiscussion;
// BoqReceived (a priced BoQ arrived) supersedes the former Quoted; Selected is reached
// via selection (which rejects rivals); Withdrawn is terminal.
export const BID_STATUSES: readonly BidStatus[] = [
  "InDiscussion",
  "BoqExpected",
  "BoqReceived",
  "Shortlisted",
  "Selected",
  "Rejected",
  "Withdrawn",
];

export const BID_STATUS_LABELS: Record<BidStatus, string> = {
  InDiscussion: t("enum.bidStatus.InDiscussion"),
  BoqExpected: t("enum.bidStatus.BoqExpected"),
  BoqReceived: t("enum.bidStatus.BoqReceived"),
  Shortlisted: t("enum.bidStatus.Shortlisted"),
  Selected: t("enum.bidStatus.Selected"),
  Rejected: t("enum.bidStatus.Rejected"),
  Withdrawn: t("enum.bidStatus.Withdrawn"),
};

// The kind of interaction a discussion note records.
export type NoteType = "Meeting" | "Call" | "Email" | "Note";

export const NOTE_TYPES: readonly NoteType[] = [
  "Meeting",
  "Call",
  "Email",
  "Note",
];

export const NOTE_TYPE_LABELS: Record<NoteType, string> = {
  Meeting: t("enum.noteType.Meeting"),
  Call: t("enum.noteType.Call"),
  Email: t("enum.noteType.Email"),
  Note: t("enum.noteType.Note"),
};

// A dated entry in a bid's discussion log. Immutable once logged.
export interface DiscussionNote {
  id: string;
  type: NoteType;
  occurredOn: string;
  authorId: string;
  content: string;
}

export interface Bid {
  id: string;
  workPackageId: string;
  contractorId: string;
  status: BidStatus;
  firstContactedOn?: string | null;
  expectedBoqDate?: string | null;
  summary?: string | null;
  notes: DiscussionNote[]; // oldest first
  createdAt: string;
}

export async function getBids(workPackageId: string): Promise<Bid[]> {
  const res = await fetch(
    `${apiBaseUrl()}/api/work-packages/${workPackageId}/bids`,
    { cache: "no-store" },
  );
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`);
  }
  return res.json();
}

export async function getBid(id: string): Promise<Bid | null> {
  const res = await fetch(`${apiBaseUrl()}/api/bids/${id}`, {
    cache: "no-store",
  });
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`);
  }
  return res.json();
}

// Bills of quantities ----------------------------------------------------
//
// A bill of quantities (BoQ, RO "deviz") is a contractor's priced, itemized cost
// estimate submitted within a bid. There is at most one BoQ per bid; a revised deviz
// replaces its contents in place rather than creating another version. It is the
// aggregate root, addressable by its own id, and owns its Sections (e.g. Foundation,
// Roof) which in turn own priced Line items. Every amount is stored in one
// `pricingCurrency` (fixed at draft); the other currency is derived via the optional
// pinned `exchangeRate`. See docs/architecture/domain-model.md.

export type Currency = "RON" | "EUR";

export const CURRENCIES: readonly Currency[] = ["RON", "EUR"];

export type BoqStatus =
  | "Draft"
  | "Submitted"
  | "Accepted"
  | "Rejected"
  | "Withdrawn";

// The statuses in lifecycle order, plus human-readable labels. Shared by the status
// control and the BoQ tables so they never drift apart. A BoQ is born Draft; Accepted
// is the basis for a contract; Rejected and Withdrawn are terminal (closed) states.
export const BOQ_STATUSES: readonly BoqStatus[] = [
  "Draft",
  "Submitted",
  "Accepted",
  "Rejected",
  "Withdrawn",
];

export const BOQ_STATUS_LABELS: Record<BoqStatus, string> = {
  Draft: t("enum.boqStatus.Draft"),
  Submitted: t("enum.boqStatus.Submitted"),
  Accepted: t("enum.boqStatus.Accepted"),
  Rejected: t("enum.boqStatus.Rejected"),
  Withdrawn: t("enum.boqStatus.Withdrawn"),
};

// A monetary amount in a single currency (mirrors the Money value object).
export interface Money {
  amount: number;
  currency: Currency;
}

// A pinned EUR↔RON conversion rate (mirrors the ExchangeRate value object). `asOf`
// is a plain `yyyy-MM-dd` date.
export interface ExchangeRate {
  baseCurrency: Currency;
  quoteCurrency: Currency;
  rate: number;
  asOf: string;
}

// The default VAT rate (percent) applied to a line item — Romania's standard rate.
export const DEFAULT_VAT_RATE_PERCENTAGE = 21;

// A priced row within a section. `unitPrice` / `lineTotal` are net (VAT-exclusive);
// `vatRatePercentage` (21 by default) yields the derived `unitPriceWithVat` /
// `lineTotalWithVat` (VAT-inclusive). `lineTotal` is derived (quantity × unit price).
export interface LineItem {
  id: string;
  description: string;
  quantity: number;
  unitOfMeasureId: string;
  unitPrice: Money;
  vatRatePercentage: number;
  unitPriceWithVat: Money;
  lineTotal: Money;
  lineTotalWithVat: Money;
  sequence: number;
  notes?: string | null;
}

// A fixed second-level grouping of line items inside a Section. `subtotal` (net) /
// `subtotalWithVat` (gross) are derived (sum of the line totals).
export interface Subsection {
  id: string;
  name: string;
  sequence: number;
  description?: string | null;
  subtotal: Money;
  subtotalWithVat: Money;
  lineItems: LineItem[];
}

// A grouping of line items inside a BoQ. `subtotal` (net) / `subtotalWithVat` (gross)
// are derived: the section's direct line totals plus its subsections' subtotals.
export interface Section {
  id: string;
  name: string;
  sequence: number;
  description?: string | null;
  subtotal: Money;
  subtotalWithVat: Money;
  lineItems: LineItem[]; // held directly in the section (not in a subsection)
  subsections: Subsection[];
}

export interface BillOfQuantities {
  id: string;
  bidId: string;
  reference?: string | null;
  status: BoqStatus;
  pricingCurrency: Currency;
  exchangeRate?: ExchangeRate | null;
  submittedOn?: string | null;
  validUntil?: string | null;
  total: Money; // derived: sum of section subtotals (net, VAT-exclusive)
  totalWithVat: Money; // derived: sum of section subtotals (gross, VAT-inclusive)
  sections: Section[];
  createdAt: string;
}

// The single BoQ for a bid, or null if none has been drafted yet (at most one per bid).
export async function getBidBoq(
  bidId: string,
): Promise<BillOfQuantities | null> {
  const res = await fetch(
    `${apiBaseUrl()}/api/bids/${bidId}/bills-of-quantities`,
    { cache: "no-store" },
  );
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(
      `${res.status} ${res.statusText}`,
    );
  }
  return res.json();
}

export async function getBillOfQuantities(
  id: string,
): Promise<BillOfQuantities | null> {
  const res = await fetch(`${apiBaseUrl()}/api/bills-of-quantities/${id}`, {
    cache: "no-store",
  });
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(
      `${res.status} ${res.statusText}`,
    );
  }
  return res.json();
}

// Contracts --------------------------------------------------------------
//
// A contract is the award for a work package: created when one bid is selected and
// its BoQ accepted. It references the work package and the accepted BoQ by id (the
// bid and contractor are reached through that BoQ), carries an agreed `value`
// (defaulting to the BoQ total) and its own lifecycle that evolves after the award
// (Draft → Signed → Active → Completed / Terminated). A work package has at most one
// contract. See docs/architecture/domain-model.md.

export type ContractStatus =
  | "Draft"
  | "Signed"
  | "Active"
  | "Completed"
  | "Terminated";

// The statuses in lifecycle order, plus human-readable labels. Shared by the status
// control and the contracts table so they never drift apart. A contract is born Draft;
// Completed and Terminated are terminal (closed) — the backend forbids transitioning
// out of them.
export const CONTRACT_STATUSES: readonly ContractStatus[] = [
  "Draft",
  "Signed",
  "Active",
  "Completed",
  "Terminated",
];

export const CONTRACT_STATUS_LABELS: Record<ContractStatus, string> = {
  Draft: t("enum.contractStatus.Draft"),
  Signed: t("enum.contractStatus.Signed"),
  Active: t("enum.contractStatus.Active"),
  Completed: t("enum.contractStatus.Completed"),
  Terminated: t("enum.contractStatus.Terminated"),
};

export interface Contract {
  id: string;
  workPackageId: string;
  acceptedBoqId: string;
  contractNumber?: string | null;
  status: ContractStatus;
  value: Money;
  signedOn?: string | null;
  startDate?: string | null;
  plannedEndDate?: string | null;
  actualEndDate?: string | null;
  notes?: string | null;
  createdAt: string;
}

export async function getContracts(): Promise<Contract[]> {
  const res = await fetch(`${apiBaseUrl()}/api/contracts`, {
    cache: "no-store",
  });
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`);
  }
  return res.json();
}

export async function getContract(id: string): Promise<Contract | null> {
  const res = await fetch(`${apiBaseUrl()}/api/contracts/${id}`, {
    cache: "no-store",
  });
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`);
  }
  return res.json();
}

// The (at most one) contract awarded for a work package, or null if it has none yet.
export async function getContractByWorkPackage(
  workPackageId: string,
): Promise<Contract | null> {
  const res = await fetch(
    `${apiBaseUrl()}/api/work-packages/${workPackageId}/contract`,
    { cache: "no-store" },
  );
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`);
  }
  return res.json();
}

// Project budget ---------------------------------------------------------
//
// A read model assembled by the backend across work packages, their bids' bills
// of quantities, and any awarded contract — the project's projected cost. Each
// work-package line picks its figure server-side: an awarded contract's value
// wins; otherwise the range of received BoQ totals; otherwise it is pending (bids
// but no price) or has none. Money is reported per currency (RON/EUR are never
// summed together). All figures are VAT-inclusive (gross). See
// docs/specifications/project-budget-view.md.

// How a work-package line derives its figure (mirrors the BudgetLineKind enum).
export type BudgetLineKind = "Contract" | "Bids" | "Pending" | "None";

// The spread of candidate bid prices for one work package in one currency. `low`/
// `high` are VAT-inclusive (gross) BoQ totals. `bidCount` is how many bids in this
// currency carry a priced BoQ.
export interface CandidateRange {
  currency: Currency;
  low: Money;
  high: Money;
  bidCount: number;
}

// A work-package figure as an approximate EUR (gross) band: low === high for an
// awarded line, the converted candidate band for a bid line.
export interface EurBand {
  low: Money;
  high: Money;
}

export interface WorkPackageBudgetLine {
  workPackageId: string;
  name: string;
  status: WorkPackageStatus;
  sequence: number;
  kind: BudgetLineKind;
  committed?: Money | null; // set only when kind === "Contract"
  candidates: CandidateRange[]; // one per currency, only when kind === "Bids"
  eurEquivalent?: EurBand | null; // approximate EUR (gross); null when no figure
}

// Project-level projection for one currency: committed contracts, the estimated
// band for work packages still out to bid, and the projected band (committed +
// estimated). All net (VAT-exclusive).
export interface CurrencyTotals {
  currency: Currency;
  committed: Money;
  estimatedLow: Money;
  estimatedHigh: Money;
  projectedLow: Money;
  projectedHigh: Money;
}

// The per-currency totals converted to EUR and summed into one comparable figure,
// using a single app-wide display rate (`ronPerEur`, "1 EUR = N RON"). Approximate —
// the per-BoQ pinned rate stays the source of truth for a specific quote.
export interface EurEquivalent {
  ronPerEur: number;
  totals: CurrencyTotals; // currency === "EUR"
}

export interface ProjectBudget {
  projectId: string;
  projectName: string;
  lines: WorkPackageBudgetLine[];
  totalsByCurrency: CurrencyTotals[];
  unpricedWorkPackageCount: number; // work packages with no figure (pending / no bids)
  eurEquivalent?: EurEquivalent | null; // null when there are no figures to convert
}

export async function getProjectBudget(
  projectId: string,
): Promise<ProjectBudget | null> {
  const res = await fetch(`${apiBaseUrl()}/api/projects/${projectId}/budget`, {
    cache: "no-store",
  });
  if (res.status === 404) {
    return null;
  }
  if (!res.ok) {
    throw new Error(`${res.status} ${res.statusText}`);
  }
  return res.json();
}
