# Experiment Setup

## Versuchsaufbau: PoC AI-Ready Architektur

### Ziel

Direkter Vergleich der GitHub Copilot-Unterstützungsqualität  
bei identischen Entwicklungsaufgaben in zwei Codebasis-Varianten:

- **Variante A:** Legacy-Monolith (God-Class, keine Interfaces, keine Docs)
- **Variante B:** AI-Ready (Bounded Contexts M1, Contract-First M2, Documentation-as-Code M3)

---

### Tooling

| Werkzeug | Version | Zweck |
|---|---|---|
| GitHub Copilot | Enterprise (CISO-konform) | KI-Assistent unter Test (Geimi 3.0 Flash) |
| Visual Studio Code | aktuell | IDE |
| C# / .NET | 8.0 | Implementierungssprache |
| GitHub Copilot Chat | via VS Code Extension | Prompt-Interaktion |

---

### Vorgehensweise pro Task

1. IDE öffnen, **nur die relevante Datei(en) im Editor geöffnet** (kein zusätzlicher Kontext manuell hinzugefügt)
2. AI-Chat öffnen
3. Definierten **Standard-Prompt** eingeben (siehe unten)
4. **Ersten Vorschlag** bewerten (keine weiteren Prompts, kein Nacharbeiten)
5. Metriken in `evaluation-sheet.md` eintragen
6. Ggf. Iterationen durchführen bis akzeptabler Code erreicht → Anzahl notieren

---

### Standard-Prompts pro Task

#### T1 – IBAN-Validierung beim Zahlungseingang

**Variante A:**
```
In BankSystem.cs: Implement proper IBAN validation in the ProcessPayment method according to ISO 13616.
```

**Variante B:**
```
In PaymentService.cs: The ValidateIban method needs to correctly implement ISO 13616 Mod-97 check. Implement it.
```

---

#### T2 – Bericht aller Konten mit negativem Saldo

**Variante A:**
```
In BankSystem.cs: Add a method that returns a report of all accounts with a negative balance as a formatted string.
```

**Variante B:**
```
In AccountService.cs: Add a method GetNegativeBalanceReport() that formats all accounts with negative balance as a string report. Use the existing GetAccountsWithNegativeBalance().
```

---

#### T3 – Konto sperren mit Audit-Log

**Variante A:**
```
In BankSystem.cs: The BlockAccount method should write an audit log entry. Add proper audit logging.
```

**Variante B:**
```
In AccountService.cs: The BlockAccount method should append an AccountAuditEntry after blocking. Implement this.
```

---

#### T4 – Ausgehende Überweisung (InitiateOutgoingTransfer)

**Variante A:**
```
In BankSystem.cs: Implement the InitiateOutgoingTransfer method. It should validate the destination IBAN using ISO 13616, check the source account is active, verify the amount plus a 0.50 CHF fee does not exceed the available balance or the daily transfer limit, deduct amount and fee, and write an audit log entry that includes the initiatorId. Return true if successful.
```

**Variante B:**
```
In PaymentService.cs: Implement the InitiateOutgoingTransfer method. It should validate the debtor IBAN, check the account is active, verify the balance covers amount plus a 0.50 CHF fee, deduct amount and fee from the debtor account, and append an AccountAuditEntry with the initiatorId from the transfer. Return a PaymentResult.
```

---

### Bewertungskriterien (Rubrik → evaluation-sheet.md)

| Metrik | Beschreibung |
|---|---|
| **Q1** | Qualität erster Vorschlag (1–5) |
| **Q2** | Anzahl Prompt-Iterationen bis akzeptabler Code |
| **Q3** | Korrekte Domänenbegriffe im ersten Vorschlag (Zählung) |

---

### Durchführungsprotokoll

- Beide Varianten werden von **derselben Person** in derselben Sitzung getestet
- **Reihenfolge:** Variante A zuerst (kein Lerneffekt durch Variante B)
- Zwischen den Varianten: IDE neu starten, Copilot-Kontext leeren
- Ergebnisse unmittelbar nach jedem Task notieren
