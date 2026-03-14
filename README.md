# PoC – AI-Ready Architektur für Kernbanken-Monolithen

## Zweck

Dieser Proof-of-Concept begleitet die Transferarbeit  
**"AI-Ready Architektur- und Arbeitsweisen für grosse Kernbanken-Monolithen"**  
(HSLU CAS Modern Software Engineering & Development, 2025, Reto Widmer / Niklas Killenberger).

Ziel ist der direkte Vergleich zweier extremer Varianten hinsichtlich der Unterstützungsqualität von  
GitHub Copilot bei drei definierten Entwicklungsaufgaben.

---

## Varianten

| Merkmal | Variante A (Legacy) | Variante B (AI-Ready) |
|---|---|---|
| Architektur | God-Class, keine Interfaces | Modulare Bounded Contexts (M1) |
| API-Spezifikation | keine | OpenAPI 3.1 Contract-First (M2) |
| Dokumentation | keine | ADRs + XML-Docs + README (M3) |
| Namensgebung | technisch / generisch | domänensprachlich (Ubiquitous Language) |

---

## Experiment-Tasks

| # | Task | Domäne |
|---|---|---|
| T1 | IBAN-Validierung beim Zahlungseingang implementieren | Payment |
| T2 | Bericht aller Konten mit negativem Saldo erstellen | Account |
| T3 | Methode zur Kontogesperrung mit Audit-Log implementieren | Account |

---

## Metriken

- **Q1** Qualität des ersten Copilot-Vorschlags (1–5, Rubrik in `experiment/evaluation-sheet.md`)
- **Q2** Anzahl Iterationen bis akzeptabler Code
- **Q3** Korrektheit der Domänenbegriffe im ersten Vorschlag (Zählung)

---

## Verzeichnisstruktur

```
poc/
├── README.md                  ← dieser Text
├── experiment/
│   ├── setup.md               ← Versuchsaufbau, Tooling, Vorgehensweise
│   └── evaluation-sheet.md    ← Bewertungsrubrik + Ergebnistabellen
├── variant-a/                 ← Legacy Monolith
│   ├── BankApp.csproj
│   ├── Program.cs
│   └── BankSystem.cs          ← God-Class
└── variant-b/                 ← AI-Ready
    ├── BankApp.csproj
    ├── Program.cs
    ├── Account/
    │   ├── IAccountService.cs
    │   ├── AccountService.cs
    │   └── AccountModels.cs
    ├── Payment/
    │   ├── IPaymentService.cs
    │   ├── PaymentService.cs
    │   └── PaymentModels.cs
    ├── Customer/
    │   ├── ICustomerService.cs
    │   └── CustomerModels.cs
    ├── contracts/
    │   ├── account-api.yaml
    │   └── payment-api.yaml
    └── docs/
        ├── adr-001-bounded-contexts.md
        ├── adr-002-contract-first.md
        └── adr-003-documentation-as-code.md
```
