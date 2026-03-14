# Evaluation Sheet – PoC-Experiment

## Bewertungsrubrik Q1: Qualität des ersten Copilot-Vorschlags

| Wert | Bedeutung |
|---|---|
| **5** | Vorschlag ist vollständig korrekt, domänensprachlich, keine Nacharbeit nötig |
| **4** | Kleiner Fehler oder fehlender Detail, 1 Iteration nötig |
| **3** | Grundstruktur korrekt, aber wesentliche Teile fehlen oder sind falsch |
| **2** | Nur Grundgerüst korrekt, grosse Teile müssen neu geschrieben werden |
| **1** | Vorschlag ist nicht verwendbar, falsches Konzept oder falsche Domäne |

---

## Ergebnistabellen

### T1 – IBAN-Validierung beim Zahlungseingang

| Metrik | Variante A (Legacy) | Variante B (AI-Ready) | Delta |
|---|---|---|---|
| Q1 Qualität erster Vorschlag (1–5) | ___ | ___ | ___ |
| Q2 Anzahl Iterationen | ___ | ___ | ___ |
| Q3 Korrekte Domänenbegriffe (Anzahl) | ___ | ___ | ___ |
| Notizen / Beobachtungen | | | |

**Copilot-Vorschlag Variante A (erster Vorschlag, gekürzt):**
```csharp
// [Hier einfügen nach Durchführung]
```

**Copilot-Vorschlag Variante B (erster Vorschlag, gekürzt):**
```csharp
// [Hier einfügen nach Durchführung]
```

---

### T2 – Bericht aller Konten mit negativem Saldo

| Metrik | Variante A (Legacy) | Variante B (AI-Ready) | Delta |
|---|---|---|---|
| Q1 Qualität erster Vorschlag (1–5) | ___ | ___ | ___ |
| Q2 Anzahl Iterationen | ___ | ___ | ___ |
| Q3 Korrekte Domänenbegriffe (Anzahl) | ___ | ___ | ___ |
| Notizen / Beobachtungen | | | |

**Copilot-Vorschlag Variante A (erster Vorschlag, gekürzt):**
```csharp
// [Hier einfügen nach Durchführung]
```

**Copilot-Vorschlag Variante B (erster Vorschlag, gekürzt):**
```csharp
// [Hier einfügen nach Durchführung]
```

---

### T3 – Konto sperren mit Audit-Log

| Metrik | Variante A (Legacy) | Variante B (AI-Ready) | Delta |
|---|---|---|---|
| Q1 Qualität erster Vorschlag (1–5) | ___ | ___ | ___ |
| Q2 Anzahl Iterationen | ___ | ___ | ___ |
| Q3 Korrekte Domänenbegriffe (Anzahl) | ___ | ___ | ___ |
| Notizen / Beobachtungen | | | |

**Copilot-Vorschlag Variante A (erster Vorschlag, gekürzt):**
```csharp
// [Hier einfügen nach Durchführung]
```

**Copilot-Vorschlag Variante B (erster Vorschlag, gekürzt):**
```csharp
// [Hier einfügen nach Durchführung]
```

---

## Gesamtübersicht (nach Durchführung ausfüllen)

| Task | Q1-A | Q1-B | Q1-Delta | Q2-A | Q2-B | Q2-Delta | Q3-A | Q3-B | Q3-Delta |
|---|---|---|---|---|---|---|---|---|---|
| T1 IBAN-Validierung | | | | | | | | | |
| T2 Negativsaldo-Report | | | | | | | | | |
| T3 Kontosperrung + Audit | | | | | | | | | |
| **Ø Mittelwert** | | | | | | | | | |

---

## Q3 Domänenbegriff-Zählung – Referenzliste

Folgende Begriffe gelten als "korrekte Domänenbegriffe" (aus Ubiquitous Language):

| Begriff | Variante A vorhanden? | Variante B vorhanden? |
|---|---|---|
| `iban` / `Iban` | | |
| `Girokonto` / `Sparkonto` | | |
| `BlockReason` | | |
| `RemittanceInfo` / `Verwendungszweck` | | |
| `AccountStatus` | | |
| `IncomingPayment` | | |
| `AuditEntry` / `AuditLog` | | |
| `InitiatedBy` | | |
| `creditor` / `debtor` | | |
| `NegativeBalance` | | |

**Zählmethode:** Wörter, die im ersten Copilot-Vorschlag erscheinen und
dem obigen Glossar entsprechen (Gross-/Kleinschreibung ignoriert).

---

## Interpretationshinweise

- **Q1-Delta > 1.5:** Substanzielle Qualitätsverbesserung durch AI-Ready-Massnahmen
- **Q2-Delta > 2:** Signifikante Zeitersparnis durch weniger Iterationen
- **Q3-Delta > 3:** Copilot übernimmt Domänensprache aus Kontext-Artefakten
