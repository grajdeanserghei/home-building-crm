// Centralized access to the .NET backend.
//
// In Aspire, the AppHost injects the backend URL as the API_BASE_URL env var
// (see AppHost.cs -> WithEnvironment("API_BASE_URL", ...)). When running the
// Next.js app standalone (outside Aspire), it falls back to localhost.
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
  Planned: "Planned",
  InProgress: "In Progress",
  OnHold: "On Hold",
  Completed: "Completed",
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
    throw new Error(`Failed to load projects: ${res.status} ${res.statusText}`);
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
    throw new Error(`Failed to load project: ${res.status} ${res.statusText}`);
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

// The status values are surfaced read-only for now: the backend deliberately omits
// status from the edit command, since lifecycle transitions (award, etc.) carry
// invariants and get dedicated endpoints. Labels keep the table and any badge in sync.
export const WORK_PACKAGE_STATUS_LABELS: Record<WorkPackageStatus, string> = {
  Defined: "Defined",
  OpenForBids: "Open for Bids",
  Awarded: "Awarded",
  InProgress: "In Progress",
  Completed: "Completed",
  Cancelled: "Cancelled",
};

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
  createdAt: string;
}

export async function getWorkPackages(projectId: string): Promise<WorkPackage[]> {
  const res = await fetch(
    `${apiBaseUrl()}/api/projects/${projectId}/work-packages`,
    { cache: "no-store" },
  );
  if (!res.ok) {
    throw new Error(
      `Failed to load work packages: ${res.status} ${res.statusText}`,
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
      `Failed to load work package: ${res.status} ${res.statusText}`,
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
  createdAt: string;
}

export async function getContractors(): Promise<Contractor[]> {
  const res = await fetch(`${apiBaseUrl()}/api/contractors`, {
    cache: "no-store",
  });
  if (!res.ok) {
    throw new Error(
      `Failed to load contractors: ${res.status} ${res.statusText}`,
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
      `Failed to load contractor: ${res.status} ${res.statusText}`,
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
  Length: "Length",
  Area: "Area",
  Volume: "Volume",
  Mass: "Mass",
  Count: "Count",
  Time: "Time",
  Other: "Other",
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
      `Failed to load units of measure: ${res.status} ${res.statusText}`,
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
      `Failed to load unit of measure: ${res.status} ${res.statusText}`,
    );
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
  | "Quoted"
  | "Shortlisted"
  | "Selected"
  | "Rejected"
  | "Withdrawn";

// The statuses in lifecycle order, plus human-readable labels. Shared by the status
// control and the bids table so they never drift apart. A bid is opened InDiscussion;
// Selected is reached via selection (which rejects rivals); Withdrawn is terminal.
export const BID_STATUSES: readonly BidStatus[] = [
  "InDiscussion",
  "Quoted",
  "Shortlisted",
  "Selected",
  "Rejected",
  "Withdrawn",
];

export const BID_STATUS_LABELS: Record<BidStatus, string> = {
  InDiscussion: "In Discussion",
  Quoted: "Quoted",
  Shortlisted: "Shortlisted",
  Selected: "Selected",
  Rejected: "Rejected",
  Withdrawn: "Withdrawn",
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
  Meeting: "Meeting",
  Call: "Call",
  Email: "Email",
  Note: "Note",
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
    throw new Error(`Failed to load bids: ${res.status} ${res.statusText}`);
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
    throw new Error(`Failed to load bid: ${res.status} ${res.statusText}`);
  }
  return res.json();
}
