// The single Romanian message catalog. Every user-facing string lives here, keyed by a
// stable dot-notation key, so the four stakeholders can review terminology in one place.
// Looked up via the t() helper (see ./index.ts), which interpolates {placeholder} tokens.
//
// This app ships Romanian-only — there is no language switcher. The catalog is kept as a
// plain dictionary (not hardcoded JSX) so adding an `en` catalog + switcher later is
// additive rather than a rewrite. See docs/specifications/romanian-localization.md.
//
// Conventions:
//   - `glossary.*`  — the ubiquitous-language domain terms (reuse these, don't re-translate).
//   - `enum.*`      — labels for domain enum members, keyed by the English wire value.
//   - `common.*`    — shared UI verbs/labels reused across pages.
//   - `nav.*`/`meta.*` — chrome (navigation, document metadata).
//   - `errors.*`    — Romanian templates for known domain-exception codes (see backend).
//   - everything else is grouped by route/feature (e.g. `projects.*`, `lineItem.*`).
export const ro = {
  // — Ubiquitous-language glossary (domain terms) ————————————————————————
  "glossary.project": "Proiect",
  "glossary.workPackage": "Pachet de lucrări",
  "glossary.scopeItem": "Articol de scop",
  "glossary.contractor": "Antreprenor",
  "glossary.bid": "Ofertă",
  "glossary.boq": "Listă de cantități",
  "glossary.section": "Secțiune",
  "glossary.lineItem": "Articol",
  "glossary.contract": "Contract",
  "glossary.unitOfMeasure": "Unitate de măsură",
  "glossary.discussionNote": "Notă de discuție",
  "glossary.exchangeRate": "Curs valutar",
  "glossary.vat": "TVA",
  "glossary.vatRate": "Cotă TVA",
  "glossary.money": "Sumă",
  "glossary.dueDate": "Termen",
  "glossary.created": "Creat",
  "glossary.updated": "Actualizat",

  // — Enum labels (keyed by the English wire value; the wire stays English) ——————
  "enum.projectStatus.Planned": "Planificat",
  "enum.projectStatus.InProgress": "În desfășurare",
  "enum.projectStatus.OnHold": "Suspendat",
  "enum.projectStatus.Completed": "Finalizat",

  "enum.workPackageStatus.Defined": "Definit",
  "enum.workPackageStatus.OpenForBids": "Deschis pentru oferte",
  "enum.workPackageStatus.Awarded": "Atribuit",
  "enum.workPackageStatus.InProgress": "În desfășurare",
  "enum.workPackageStatus.Completed": "Finalizat",
  "enum.workPackageStatus.Cancelled": "Anulat",

  "enum.scopeItemRequirement.Mandatory": "Obligatoriu",
  "enum.scopeItemRequirement.Optional": "Opțional",

  "enum.bidStatus.InDiscussion": "În discuție",
  "enum.bidStatus.BoqExpected": "Deviz așteptat",
  "enum.bidStatus.BoqReceived": "Deviz primit",
  "enum.bidStatus.Shortlisted": "Preselectat",
  "enum.bidStatus.Selected": "Selectat",
  "enum.bidStatus.Rejected": "Respins",
  "enum.bidStatus.Withdrawn": "Retras",

  "enum.noteType.Meeting": "Întâlnire",
  "enum.noteType.Call": "Apel telefonic",
  "enum.noteType.Email": "Email",
  "enum.noteType.Note": "Notă",

  "enum.boqStatus.Draft": "Ciornă",
  "enum.boqStatus.Submitted": "Trimis",
  "enum.boqStatus.Accepted": "Acceptat",
  "enum.boqStatus.Rejected": "Respins",
  "enum.boqStatus.Withdrawn": "Retras",

  "enum.contractStatus.Draft": "Ciornă",
  "enum.contractStatus.Signed": "Semnat",
  "enum.contractStatus.Active": "Activ",
  "enum.contractStatus.Completed": "Finalizat",
  "enum.contractStatus.Terminated": "Reziliat",

  "enum.unitCategory.Length": "Lungime",
  "enum.unitCategory.Area": "Suprafață",
  "enum.unitCategory.Volume": "Volum",
  "enum.unitCategory.Mass": "Masă",
  "enum.unitCategory.Count": "Bucăți",
  "enum.unitCategory.Time": "Timp",
  "enum.unitCategory.Other": "Altele",

  // — Common UI (verbs, shared labels, states) ————————————————————————————
  "common.actions": "Acțiuni",
  "common.edit": "Editează",
  "common.delete": "Șterge",
  "common.save": "Salvează",
  "common.saveChanges": "Salvează modificările",
  "common.cancel": "Anulează",
  "common.create": "Creează",
  "common.add": "Adaugă",
  "common.remove": "Elimină",
  "common.back": "Înapoi",
  "common.optional": "opțional",
  "common.name": "Nume",
  "common.description": "Descriere",
  "common.status": "Stare",
  "common.notes": "Note",
  "common.created": "Creat",
  "common.noResults": "Niciun rezultat.",
  "common.unknownError": "Eroare necunoscută",
  "common.apiError": "Nu s-a putut contacta API-ul: {error}",
  "common.actionError": "Acțiunea nu a putut fi finalizată: {error}",
  "common.notFound": "Nu a fost găsit.",
  "common.yes": "Da",
  "common.no": "Nu",

  // — Navigation & document metadata ——————————————————————————————————————
  "meta.title": "Gestionarea Proiectului Casei",
  "meta.description": "Urmărește lucrările la construcția casei",
  "nav.brand": "Gestionarea Proiectului Casei",
  "nav.projects": "Proiecte",
  "nav.contractors": "Antreprenori",
  "nav.contracts": "Contracte",
  "nav.unitsOfMeasure": "Unități de măsură",

  // — Domain-exception codes → Romanian templates ————————————————————————————
  // Keyed by the stable `code` the backend puts on the ProblemDetails. {params} are
  // re-interpolated from the response's `params` extension. Codes not listed here fall
  // back to the English `detail` (partial coverage is safe — see the localization spec).
  "errors.ScopeItemNameDuplicate":
    "Există deja un articol de scop cu numele „{name}” în acest pachet de lucrări.",
  "errors.BoqClosed":
    "O listă de cantități în starea {status} este închisă și nu își poate schimba starea.",
  "errors.BoqNotEditable":
    "O listă de cantități în starea {status} nu mai poate fi editată.",
  "errors.BoqExchangeRateCurrencyMismatch":
    "Cursul valutar fixat trebuie să implice moneda de tarifare ({pricingCurrency}).",
  "errors.LineItemCurrencyMismatch":
    "Moneda prețului articolului ({lineCurrency}) trebuie să corespundă monedei de tarifare a listei ({billCurrency}).",
  "errors.BidInvalidStatusTransition":
    "O ofertă nu poate trece din starea {from} în starea {to}.",
  "errors.ContractClosed":
    "Un contract în starea {status} este închis și nu se mai poate modifica.",

  // — Projects (home list, detail, form) ——————————————————————————————————
  "projects.new": "Proiect nou",
  "projects.add": "Adaugă proiect",
  "projects.title": "Proiecte",
  "projects.empty": "Niciun proiect încă. Adaugă primul mai sus.",
  "projects.col.due": "Termen",
  "projects.namePlaceholder": "Numele proiectului",
  "projects.descriptionPlaceholder": "Descriere (opțional)",
  "projects.backToAll": "← Toate proiectele",
  "projects.workPackagesSubtitle": "Pachetele de lucrări ale acestui proiect.",
  "projects.workPackages": "Pachete de lucrări",
  "projects.edit": "Editează proiectul",
  "projects.editSubtitle": "Actualizează detaliile pentru „{name}”.",
  "projects.deleteTitle": "Ștergi proiectul?",
  "projects.deleteBodyBefore": "Această acțiune va șterge definitiv",
  "projects.deleteBodyAfter": ". Acțiunea nu poate fi anulată.",

  // — Work packages (detail page on the project, table columns) ————————————
  "workPackages.new": "Pachet de lucrări nou",
  "workPackages.add": "Adaugă pachet de lucrări",
  "workPackages.empty": "Niciun pachet de lucrări încă. Definește primul mai sus.",
  "workPackages.col.plannedStart": "Început planificat",
  "workPackages.col.plannedEnd": "Sfârșit planificat",
  "workPackages.backToProject": "← Înapoi la proiect",
  "workPackages.detailSubtitle": "Ofertele pentru acest pachet de lucrări.",
  "workPackages.contractTitle": "Contract",
  "workPackages.awardedNotice": "Acest pachet de lucrări a fost atribuit.",
  "workPackages.viewContract": "Vezi contractul →",
  "workPackages.changeStatusTitle": "Schimbă starea",
  "workPackages.statusFinal":
    "Acest pachet de lucrări este {status} — starea sa este finală.",
  "workPackages.updateStatus": "Actualizează starea",
  "workPackages.awardingHint":
    "Atribuirea se face automat când o ofertă este selectată și contractul ei este creat — nu se stabilește aici.",
  "workPackages.newBidTitle": "Ofertă nouă",
  "workPackages.noContractors":
    "Niciun antreprenor înregistrat încă — adaugă unul mai întâi în secțiunea Antreprenori.",
  "workPackages.allContractorsBid":
    "Fiecare antreprenor înregistrat are deja o ofertă pentru acest pachet de lucrări.",
  "workPackages.openBid": "Deschide ofertă",
  "workPackages.bidsTitle": "Oferte",
  "workPackages.bidsEmpty":
    "Nicio ofertă încă. Deschide una cu un antreprenor mai sus.",
  "workPackages.bidContractor": "Antreprenor",
  "workPackages.bidFirstContact": "Primul contact",
  "workPackages.bidView": "Vezi",
  "workPackages.unknownContractor": "Antreprenor necunoscut",
  "workPackages.edit": "Editează pachetul de lucrări",
  "workPackages.editSubtitle": "Actualizează detaliile pentru „{name}”.",
  "workPackages.namePlaceholder": "Numele pachetului de lucrări (ex. La Roșu)",
  "workPackages.descriptionPlaceholder": "Note despre scop (opțional)",
  "workPackages.orderPlaceholder": "Ordine",
  "workPackages.plannedStart": "Început planificat",
  "workPackages.plannedEnd": "Sfârșit planificat",

  // — Project budget (cost rollup per work package) ——————————————————————
  "budget.link": "Buget",
  "budget.backToProject": "← Înapoi la proiect",
  "budget.title": "Buget — {name}",
  "budget.subtitle":
    "Costul proiectat al lucrării, pe pachete de lucrări: valoarea contractată acolo unde s-a atribuit, altfel intervalul ofertelor primite.",
  "budget.linesTitle": "Pachete de lucrări",
  "budget.empty": "Niciun pachet de lucrări de bugetat încă.",
  "budget.col.committed": "Contractat",
  "budget.col.candidates": "Oferte candidate",
  "budget.kind.pending": "Preț în așteptare",
  "budget.kind.none": "Fără oferte",
  "budget.bidCountOne": "1 ofertă",
  "budget.bidCountMany": "{count} oferte",
  "budget.totalsTitle": "Total proiectat, pe monedă",
  "budget.totals.currency": "Monedă",
  "budget.totals.committed": "Contractat",
  "budget.totals.estimated": "Estimat (deschise)",
  "budget.totals.projected": "Total proiectat",
  "budget.unpricedNote":
    "{count} pachete de lucrări nu au încă un preț și nu sunt incluse în estimare.",
  "budget.unpricedNoteOne":
    "Un pachet de lucrări nu are încă un preț și nu este inclus în estimare.",

  // — Scope items (within a work package) ————————————————————————————————
  "scopeItems.title": "Articole de scop",
  "scopeItems.subtitle":
    "Sub-domeniile definite de proprietar pentru acest pachet de lucrări — ce trebuie făcut și ce ar putea fi eliminat sau amânat dacă bugetul este limitat.",
  "scopeItems.empty": "Niciun articol de scop încă. Adaugă primul mai jos.",
  "scopeItems.requirement": "Cerință",
  "scopeItems.add": "Adaugă articol de scop",
  "scopeItems.namePlaceholder":
    "Numele articolului de scop (ex. Încălzire pardoseală)",
  "scopeItems.orderPlaceholder": "Ordine",
  "scopeItems.notesPlaceholder": "Note (opțional)",

  // — Bids ————————————————————————————————————————————————————————————————
  "bids.unknownContractor": "Antreprenor necunoscut",
  "bids.backTo": "← Înapoi la {name}",
  "bids.workPackageFallback": "pachetul de lucrări",
  "bids.bidOn": "Ofertă pentru {name}",
  "bids.thisWorkPackage": "acest pachet de lucrări",
  "bids.firstContacted": "Primul contact",
  "bids.summary": "Rezumat",
  "bids.opened": "Deschisă",
  "bids.changeStatus": "Schimbă statusul",
  "bids.withdrawnFinal":
    "Această ofertă este retrasă — statusul ei este definitiv.",
  "bids.updateStatus": "Actualizează statusul",
  "bids.selectWinnerNote":
    "Selectarea acestei oferte drept câștigătoare respinge celelalte oferte active pentru acest pachet de lucrări.",
  "bids.boqHeading": "Liste de cantități",
  "bids.boqEmpty":
    "Nicio listă de cantități încă. Întocmește prima versiune a antreprenorului mai jos.",
  "bids.boqCol.version": "Versiune",
  "bids.boqCol.reference": "Referință",
  "bids.boqCol.totalWithVat": "Total (cu TVA)",
  "bids.view": "Vizualizează",
  "bids.draftBoqHeading": "Întocmește o listă de cantități",
  "bids.draftBoqSubmit": "Întocmește deviz",
  "bids.edit": "Editează oferta",
  "bids.editSubtitle":
    "Actualizează situația pentru „{name}”. Antreprenorul și statusul nu pot fi modificate aici (statusul are propriile controale pe ofertă).",
  "bids.thisContractor": "acest antreprenor",
  "bids.selectContractor": "Selectează antreprenorul…",
  "bids.summaryPlaceholder": "Rezumat (ex. a ofertat 120k, răspunde greu)",

  // — Discussion notes (within a bid) ——————————————————————————————————————
  "notes.logHeading": "Înregistrează o notă",
  "notes.discussionLog": "Jurnal de discuții",
  "notes.empty":
    "Nicio notă încă. Înregistrează întâlniri, apeluri și emailuri mai sus.",
  "notes.col.when": "Când",
  "notes.col.type": "Tip",
  "notes.col.note": "Notă",
  "notes.occurredOn": "A avut loc pe",
  "notes.contentPlaceholder": "Ce s-a discutat",
  "notes.logNote": "Înregistrează nota",

  // — Bills of quantities (deviz) ——————————————————————————————————————————
  "boq.title": "Deviz v{version}",
  "boq.editTitle": "Editare deviz v{version}",
  "boq.editSubtitle":
    "Actualizați antetul. Moneda de tarifare și versiunea sunt fixe; secțiunile și articolele se editează în pagina devizului.",
  "boq.backToBid": "← Înapoi la ofertă",
  "boq.backToBoq": "← Înapoi la deviz",
  "boq.subtitle": "Listă de cantități, tarifată în {currency}",
  "boq.inclVat": "cu TVA",
  "boq.exclVat": "fără TVA",
  "boq.exclShort": "fără",
  "boq.version": "Versiune",
  "boq.reference": "Referință",
  "boq.pricingCurrency": "Monedă de tarifare",
  "boq.pinnedRate": "Curs fixat",
  "boq.pinnedRateValue": "1 {base} = {rate} {quote} (la data de {asOf})",
  "boq.submittedOn": "Trimis la",
  "boq.validUntil": "Valabil până la",
  "boq.totalExclVat": "Total fără TVA",
  "boq.totalInclVat": "Total cu TVA",
  "boq.changeStatus": "Modificare stare",
  "boq.statusFinal": "Acest deviz este {status} — starea sa este finală.",
  "boq.updateStatus": "Actualizează starea",
  "boq.acceptNote":
    "Acceptarea unui deviz îl blochează împotriva editărilor ulterioare — devine baza unui contract.",
  "boq.contract": "Contract",
  "boq.underContractBefore": "Acest pachet de lucrări este sub contract (",
  "boq.underContractAfter": ").",
  "boq.viewContract": "Vezi contractul →",
  "boq.awardNote":
    "Atribuie contractul acestei oferte. Aceasta acceptă devizul, selectează oferta sa și respinge concurentele, și marchează pachetul de lucrări ca atribuit. Valoarea ia implicit totalul acestui deviz dacă este lăsată goală.",
  "boq.contractNumberPlaceholder": "Număr contract (opțional)",
  "boq.agreedValueLabel": "Valoare convenită (opțional — implicit totalul devizului)",
  "boq.currency": "Monedă",
  "boq.startDate": "Dată început",
  "boq.plannedEndDate": "Dată finalizare planificată",
  "boq.notesPlaceholder": "Note (opțional)",
  "boq.awardContract": "Atribuie contractul",
  "boq.noSectionsLocked":
    "Acest deviz nu are secțiuni și nu mai poate fi editat.",
  "boq.referencePlaceholder": "Referință / nr. deviz (opțional)",
  "boq.pinnedRateField": "Curs fixat (1 EUR = ? RON)",
  "boq.pinnedRatePlaceholder": "ex. 4,97",
  "boq.rateAsOf": "Curs la data de",

  // — Sections (within a BoQ) ——————————————————————————————————————————————
  "sections.add": "Adaugă secțiune",
  "sections.remove": "Elimină secțiunea",
  "sections.namePlaceholder": "Nume secțiune (ex. Fundație)",
  "sections.orderPlaceholder": "Ordine",

  // — Line items (within a section) ————————————————————————————————————————
  "lineItems.add": "Adaugă articol",
  "lineItems.editTitle": "Editează articolul",
  "lineItems.editSubtitle": "Modificați descrierea, cantitatea, prețul sau cota TVA.",
  "lineItems.empty": "Niciun articol încă.",
  "lineItems.noActiveUnits":
    "Nicio unitate de măsură activă — adăugați una la Unități de măsură mai întâi.",
  "lineItems.descriptionPlaceholder": "Descriere articol (ex. beton C25/30)",
  "lineItems.quantityPlaceholder": "Cantitate",
  "lineItems.selectUnit": "Selectați o unitate…",
  "lineItems.unitPriceExclVatLabel": "Preț unitar fără TVA ({currency})",
  "lineItems.vatRateLabel": "Cotă TVA (%)",
  "lineItems.col.unit": "U.M.",
  "lineItems.col.qty": "Cant.",
  "lineItems.col.unitPriceExclVat": "Preț unitar (fără TVA)",
  "lineItems.col.vat": "TVA",
  "lineItems.col.lineTotalExclVat": "Total articol (fără TVA)",
  "lineItems.col.lineTotalInclVat": "Total articol (cu TVA)",

  // — Contractors ——————————————————————————————————————————————————————————
  "contractors.title": "Antreprenori",
  "contractors.subtitle": "Firme care licitează și execută pachetele de lucrări.",
  "contractors.new": "Antreprenor nou",
  "contractors.add": "Adaugă antreprenor",
  "contractors.all": "Toți antreprenorii",
  "contractors.empty": "Niciun antreprenor încă. Adaugă-l pe primul mai sus.",
  "contractors.contact": "Contact",
  "contractors.fiscalCode": "Cod fiscal (CUI)",
  "contractors.registrationNumber": "Nr. înregistrare",
  "contractors.contactPerson": "Persoană de contact",
  "contractors.email": "Email",
  "contractors.phone": "Telefon",
  "contractors.address": "Adresă",
  "contractors.backToProjects": "Proiecte",
  "contractors.backToAll": "Toți antreprenorii",
  "contractors.detailsSubtitle": "Detaliile antreprenorului.",
  "contractors.editContractor": "Editează antreprenorul",
  "contractors.editSubtitleBefore": "Actualizează detaliile pentru ",
  "contractors.editSubtitleAfter": ".",
  "contractors.companyName": "Denumire firmă",
  "contractors.fiscalCodePlaceholder": "Cod fiscal (CUI) (opțional)",
  "contractors.registrationNumberPlaceholder": "Nr. înregistrare (J) (opțional)",
  "contractors.contactPersonPlaceholder": "Persoană de contact (opțional)",
  "contractors.emailPlaceholder": "Email (opțional)",
  "contractors.phonePlaceholder": "Telefon (opțional)",
  "contractors.streetPlaceholder": "Stradă (opțional)",
  "contractors.cityPlaceholder": "Oraș (opțional)",
  "contractors.countyPlaceholder": "Județ (opțional)",
  "contractors.postalCodePlaceholder": "Cod poștal (opțional)",
  "contractors.countryPlaceholder": "Țară (opțional)",
  "contractors.notesPlaceholder": "Note (opțional)",
  "contractors.deleteTitle": "Ștergi antreprenorul?",
  "contractors.deleteBodyBefore": "Aceasta va șterge definitiv",
  "contractors.deleteBodyAfter": ". Această acțiune nu poate fi anulată.",

  // — Contracts ————————————————————————————————————————————————————————————
  "contracts.title": "Contracte",
  "contracts.subtitle":
    "Contracte atribuite pe toate pachetele de lucrări. Un contract se creează prin atribuirea unui pachet de lucrări din lista de cantități acceptată.",
  "contracts.all": "Toate contractele",
  "contracts.empty":
    "Niciun contract încă. Atribuie unul dintr-o listă de cantități acceptată a unei oferte.",
  "contracts.workPackage": "Pachet de lucrări",
  "contracts.workPackageLower": "pachet de lucrări",
  "contracts.contractNumberShort": "Nr. contract",
  "contracts.contractNumber": "Număr contract",
  "contracts.contractNumberPlaceholder": "Număr contract (opțional)",
  "contracts.value": "Valoare",
  "contracts.signedShort": "Semnat",
  "contracts.view": "Vezi",
  "contracts.contractor": "Antreprenor",
  "contracts.acceptedBoq": "Listă de cantități acceptată",
  "contracts.inclVat": "{amount} cu TVA",
  "contracts.agreedValue": "Valoare convenită",
  "contracts.signedOn": "Semnat la",
  "contracts.startDate": "Dată început",
  "contracts.plannedEndDate": "Sfârșit planificat",
  "contracts.actualEndDate": "Sfârșit efectiv",
  "contracts.awarded": "Atribuit",
  "contracts.titleNumbered": "Contract {number}",
  "contracts.titleFor": "Contract pentru {name}",
  "contracts.backTo": "← Înapoi la {name}",
  "contracts.awardedFor": "Atribuit pentru {name}",
  "contracts.awardedContract": "Contract atribuit",
  "contracts.changeStatus": "Schimbă starea",
  "contracts.statusFinal":
    "Acest contract este {status} — starea sa este finală.",
  "contracts.newStatus": "Stare nouă",
  "contracts.signedOnHint": "Semnat la (la trecerea în Semnat)",
  "contracts.actualEndDateHint": "Sfârșit efectiv (la trecerea în Finalizat)",
  "contracts.updateStatus": "Actualizează starea",
  "contracts.statusHelp":
    "Semnarea înregistrează data semnării; finalizarea înregistrează sfârșitul efectiv. Un contract finalizat sau reziliat este închis și nu mai poate fi modificat.",
  "contracts.editTitle": "Editează contractul",
  "contracts.editSubtitle":
    "Actualizează antetul contractului. Pachetul de lucrări și lista de cantități acceptată sunt fixe; starea (semnare, finalizare) are propriile controale pe contract.",
  "contracts.currency": "Monedă",
  "contracts.valuePlaceholder": "ex. 125000",
  "contracts.notesPlaceholder": "Note (opțional)",

  // — Units of measure —————————————————————————————————————————————————————
  "unitsOfMeasure.title": "Unități de măsură",
  "unitsOfMeasure.subtitle":
    "Unități canonice folosite pentru a cuantifica liniile din lista de cantități.",
  "unitsOfMeasure.new": "Unitate nouă",
  "unitsOfMeasure.add": "Adaugă unitatea",
  "unitsOfMeasure.empty": "Nicio unitate încă. Adaug-o pe prima mai sus.",
  "unitsOfMeasure.code": "Cod",
  "unitsOfMeasure.category": "Categorie",
  "unitsOfMeasure.aliases": "Aliasuri",
  "unitsOfMeasure.active": "Activă",
  "unitsOfMeasure.inactive": "Inactivă",
  "unitsOfMeasure.activate": "Activează",
  "unitsOfMeasure.deactivate": "Dezactivează",
  "unitsOfMeasure.editUnit": "Editează unitatea",
  "unitsOfMeasure.editTitle": "Editează unitatea de măsură",
  "unitsOfMeasure.editSubtitle":
    "Actualizează detaliile pentru „{code}”. Codul este fix și nu poate fi modificat.",
  "unitsOfMeasure.backToProjects": "← Proiecte",
  "unitsOfMeasure.backToAll": "← Toate unitățile de măsură",
  "unitsOfMeasure.codePlaceholder": "Cod (ex. m, mp, buc)",
  "unitsOfMeasure.namePlaceholder": "Nume (ex. metru, metru pătrat)",
  "unitsOfMeasure.aliasesPlaceholder": "Aliasuri, separate prin virgulă (opțional)",
  "unitsOfMeasure.codeExists":
    "Există deja o unitate de măsură cu codul „{code}”.",
} as const;

export type RoCatalog = typeof ro;
