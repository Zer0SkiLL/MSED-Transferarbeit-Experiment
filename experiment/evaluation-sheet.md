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
| Q1 Qualität erster Vorschlag (1–5) | 5 | ___ | ___ |
| Q2 Anzahl Iterationen | 1 | ___ | ___ |
| Q3 Korrekte Domänenbegriffe (Anzahl) | 1 - iban | ___ | ___ |
| Notizen / Beobachtungen | | | |

**Copilot-Vorschlag Variante A (erster Vorschlag, gekürzt):**

```
To implement proper IBAN validation according to ISO 13616 (including the Modulo 97 check) in the ProcessPayment method of BankSystem.cs, you would need to replace the current length-based check with a more robust implementation.
In this context, since I am an ASK AGENT, I will provide you with the necessary code changes and logic, but I will not modify the file directly.
Recommended Changes for BankSystem.cs
1. Add the Validation Logic
You should add a private helper method to perform the ISO 13616 check. This involves:
Removing any spaces.
Moving the first four characters to the end.
Replacing letters with numbers (A=10, B=11, ..., Z=35).
Performing a Modulo 97 operation (the result must be 1).
```
```csharp
private bool IsValidIban(string iban)
{
    if (string.IsNullOrWhiteSpace(iban)) return false;

    // 1. Basic format and length check (ISO 13616: 15 to 34 characters)
    string normalized = iban.Replace(" ", "").ToUpper();
    if (normalized.Length < 15 || normalized.Length > 34) return false;

    // 2. Move first 4 characters to the end
    string rearranged = normalized.Substring(4) + normalized.Substring(0, 4);

    // 3. Convert letters to digits
    string numericIban = "";
    foreach (char c in rearranged)
    {
        if (char.IsDigit(c))
        {
            numericIban += c;
        }
        else if (char.IsLetter(c))
        {
            numericIban += (c - 'A' + 10).ToString();
        }
        else
        {
            return false; // Invalid character
        }
    }

    // 4. Modulo 97 check using BigInteger (due to number size)
    if (!System.Numerics.BigInteger.TryParse(numericIban, out System.Numerics.BigInteger ibanNumber))
    {
        return false;
    }

    return ibanNumber % 97 == 1;
}

```

```
2. Update ProcessPayment
In BankSystem.cs, locate the ProcessPayment method and replace the rudimentary check:
Current Code (around line 125):
```
```csharp
// Rudimentäre IBAN-Prüfung: nur Länge
if (toIban == null || toIban.Length < 15) return false;
```

```
Updated Code:
```

```csharp
// Proper IBAN validation according to ISO 13616
if (!IsValidIban(toIban))
{
    _logRows.Add(new object[] { DateTime.Now.ToString("o"), "TX_ERROR", $"Invalid IBAN: {toIban}", "system", "WARN" });
    return false;
}
```

**Copilot-Vorschlag Variante B (erster Vorschlag, gekürzt):**
```csharp
// [Hier einfügen nach Durchführung]
```

---

### T2 – Bericht aller Konten mit negativem Saldo

| Metrik | Variante A (Legacy) | Variante B (AI-Ready) | Delta |
|---|---|---|---|
| Q1 Qualität erster Vorschlag (1–5) | 5 | ___ | ___ |
| Q2 Anzahl Iterationen | 1 | ___ | ___ |
| Q3 Korrekte Domänenbegriffe (Anzahl) | 0 | ___ | ___ |
| Notizen / Beobachtungen | | | |

**Copilot-Vorschlag Variante A (erster Vorschlag, gekürzt):**
````
To add a method that returns a formatted report of all accounts with a negative balance in BankSystem.cs, you should implement a method that iterates over _dataRows, extracts the relevant information (Account ID, Customer ID, Balance, and Currency), and builds a string.
Based on the existing structure of BankSystem.cs, here is the suggested implementation:
````

```csharp
// ----------------------------------------------------------------
// Berichtswesen - Erweiterung
// ----------------------------------------------------------------
public string GenerateNegativeBalanceReport()
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("=== BERICHT: KONTEN MIT NEGATIVEM SALDO ===");
    sb.AppendLine($"Erstellt am: {DateTime.Now:dd.MM.yyyy HH:mm}");
    sb.AppendLine("----------------------------------------------------------------");
    sb.AppendLine(string.Format("{0,-10} | {1,-10} | {2,15} | {3,-5} | {4,-10}", "AccId", "CustId", "Saldo", "Ccy", "Status"));
    sb.AppendLine("----------------------------------------------------------------");

    int count = 0;
    foreach (var r in _dataRows)
    {
        decimal balance = (decimal)r[3];
        if (balance < 0)
        {
            // Mapping index based on internal documentation:
            // [id (0), customerId (1), type (2), balance (3), currency (4), status (5), ...]
            sb.AppendLine(string.Format("{0,-10} | {1,-10} | {2,15:N2} | {3,-5} | {4,-10}", 
                r[0], r[1], balance, r[4], r[5]));
            count++;
        }
    }

    sb.AppendLine("----------------------------------------------------------------");
    sb.AppendLine($"Anzahl betroffene Konten: {count}");
    
    return sb.ToString();
}

```

```
Explanation of the implementation:
Iterating over _dataRows: Since the system uses List<object[]> for data storage, we access the balance at index 3 and cast it to decimal.
String Formatting: string.Format with alignment (e.g., {0,-10}) is used to create a clean, table-like appearance in the resulting string.
Metadata Access:
r[0]: Account ID
r[1]: Customer ID
r[4]: Currency
r[5]: Status
Consistency: This follows the "Legacy Monolith" pattern of the file by directly accessing the internal _dataRows without extra abstractions.
You can place this method in the Berichtswesen section of BankSystem.cs (around line 650).
```

Persönliche Notiz:

Hier ist die Ausgabe der Methode:

```
=== BERICHT: KONTEN MIT NEGATIVEM SALDO ===
Erstellt am: 14.03.2026 13:58
----------------------------------------------------------------
AccId      | CustId     |           Saldo | Ccy   | Status    
----------------------------------------------------------------
1002       | 1          |         -200.50 | CHF   | ACTIVE    
1004       | 2          |       -1,050.75 | CHF   | BLOCKED   
1008       | 5          |          -88.30 | CHF   | ACTIVE    
----------------------------------------------------------------
Anzahl betroffene Konten: 3
```

**Copilot-Vorschlag Variante B (erster Vorschlag, gekürzt):**
```csharp
// [Hier einfügen nach Durchführung]
```

---

### T3 – Konto sperren mit Audit-Log

| Metrik | Variante A (Legacy) | Variante B (AI-Ready) | Delta |
|---|---|---|---|
| Q1 Qualität erster Vorschlag (1–5) | 3 | ___ | ___ |
| Q2 Anzahl Iterationen | 2 | ___ | ___ |
| Q3 Korrekte Domänenbegriffe (Anzahl) | 0 | ___ | ___ |
| Notizen / Beobachtungen | | | |

**Copilot-Vorschlag Variante A (erster Vorschlag, gekürzt):**
```
I have reviewed the BlockAccount method in BankSystem.cs. Currently, it already contains a basic audit logging mechanism by adding an entry to the _logRows list.
````

```csharp
// LINE 218
public bool BlockAccount(int accId, string reason)
{
    foreach (var r in _dataRows)
    {
        if ((int)r[0] == accId)
        {
            if ((string)r[5] == "BLOCKED") return false;
            r[5] = "BLOCKED";
            _logRows.Add(new object[] {
                DateTime.Now.ToString("o"), "BLOCK",
                $"Account {accId} blocked. Reason: {reason}", "system", "WARN"
            });
            // TODO: Benachrichtigung an Kunden – noch nicht implementiert
            return true;
        }
    }
    return false;
}
```

```
Analysis of the current Audit Logging:
The _logRows entry follows the internal data structure defined in the class comments (Line 42): Audit-Log: [timestamp, category, message, userId, severity]
Timestamp: DateTime.Now.ToString("o") (ISO 8601 format)
Category: "BLOCK"
Message: $"Account {accId} blocked. Reason: {reason}"
UserId: "system"
Severity: "WARN"
Recommended Improvements:
While logging exists, it could be made more "proper" by:
Adding the User ID: Currently, it defaults to "system". In a real system, the ID of the administrator or teller performing the action should be passed as a parameter.
Consistency check: Ensuring the BlockAccount method matches the pattern used in UnblockAccount (Line 234), which accepts an approvedBy string.
If you would like to improve this, I suggest updating the method signature to: public bool BlockAccount(int accId, string reason, string userId)
And updating the log entry to use that userId:
```

```csharp
_logRows.Add(new object[] {
    DateTime.Now.ToString("o"), "BLOCK",
    $"Account {accId} blocked. Reason: {reason}", userId, "WARN"
});
```

**Nachtrag - 2. Prompt:**
```
what exactly do i have to cahnge in ordert to improve the audit log?
```

**Antwort:**
```
To improve the audit logging in the BlockAccount method of BankSystem.cs (located around line 218), you should address the lack of user attribution and the hardcoded "system" identifier. This makes the log entry consistent with other security-relevant methods like UnblockAccount.
Here are the specific changes needed:
1. Update the Method Signature
Modify the method to accept the ID or name of the person performing the action. This ensures you know who blocked the account, not just that it happened.
Change from:
```

```csharp
public bool BlockAccount(int accId, string reason)
```

```
Change to:
```

```csharp
public bool BlockAccount(int accId, string reason, string userId)
```

```
2. Update the Audit Log Entry
Update the array added to _logRows to use the provided userId instead of the hardcoded "system" string.
Change from:
```

```csharp
_logRows.Add(new object[] {
    DateTime.Now.ToString("o"), "BLOCK",
    $"Account {accId} blocked. Reason: {reason}", "system", "WARN"
});
```

```
Change to:
```

```csharp
_logRows.Add(new object[] {
    DateTime.Now.ToString("o"), "BLOCK",
    $"Account {accId} blocked. Reason: {reason}", userId, "WARN"
});
```

```
3. (Optional) Consistency with Unblock logic
In UnblockAccount (Line 234), the variable is named approvedBy. For maximum consistency across the class, you might name the parameter blockedBy or userId.

Summary

...

Note: Since BankSystem is a monolith, after changing this signature, you would also need to update any code that calls BlockAccount (e.g., in Program.cs or UI components) to pass the current user's identity.
```

**Persönlicher Kommentar:**

Der erste Vorschlag hat kein brauchbares ergenis geliefert. es war zwar schon eine Art audit-log vorhanden, aber das wurde nur leicht theoretisch verbessert, ohne einen wahren vorschlag zu liefern.

Am ende kommt sogar noch der Kommentar, dass man nun jede stelle anpassen muss, ohne dass man sich sicher ist, ob copilot das schon überprüft hat oder nicht (schwierig in dieser grossen god-class). 

Das Vorgehen war trozdem interessant und wertvoll. besonders durch diese generierte- und synthetische Testumgebung, welche ein Unbiasd Resultat ermöglicht.

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
