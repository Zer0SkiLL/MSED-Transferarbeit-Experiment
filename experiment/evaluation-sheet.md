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
| Q1 Qualität erster Vorschlag (1–5) | 5 | 5 | 0 |
| Q2 Anzahl Iterationen | 1 | 1 | 0 |
| Q3 Korrekte Domänenbegriffe (Anzahl) | 1 | 1 | 0 |
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
```
In `PaymentService.cs`, the `ValidateIban` method already contains a basic implementation of the ISO 13616 Mod-97 check. However, to ensure it is robust and correctly handles the conversion and calculation as per the standard, here is the refined implementation logic.

The current implementation in `PaymentService.cs` (lines 65-99) is functionally correct for the Mod-97 check using `BigInteger`. Here is how the complete and correct implementation looks, including the necessary normalization and the standard-compliant algorithm:
```

```csharp
/// <inheritdoc/>
public bool ValidateIban(string iban)
{
    if (string.IsNullOrWhiteSpace(iban))
        return false;

    // 1. Normalize: remove spaces and convert to uppercase
    var normalized = iban.Replace(" ", "").ToUpperInvariant();

    // 2. Preliminary format check: 2 letters (country), 2 digits (checksum), followed by alphanumeric BBAN
    // ISO 13616 specifies a maximum length of 34 characters.
    if (normalized.Length < 5 || normalized.Length > 34 || !Regex.IsMatch(normalized, @"^[A-Z]{2}\d{2}[A-Z0-9]+$"))
        return false;

    // 3. Optional: Specific country length validation (if known)
    var countryCode = normalized[..2];
    if (IbanLengthByCountry.TryGetValue(countryCode, out int expectedLength))
    {
        if (normalized.Length != expectedLength)
            return false;
    }

    // 4. Rearrange: Move first 4 characters to the end
    // [CC][PP][BBAN...] -> [BBAN...][CC][PP]
    var rearranged = normalized[4..] + normalized[..4];

    // 5. Convert characters to digits: A=10, B=11, ..., Z=35
    var numericString = string.Concat(rearranged.Select(c =>
        char.IsLetter(c) ? (c - 'A' + 10).ToString() : c.ToString()));

    // 6. Perform Modulo-97 operation
    // The result must be 1 for the IBAN to be valid according to ISO 13616.
    if (BigInteger.TryParse(numericString, out BigInteger ibanNumber))
    {
        return ibanNumber % 97 == 1;
    }

    return false;
}
```

```
### Key steps implemented:
1.  **Normalization**: Removes whitespace and ensures uppercase.
2.  **Length and Regex Check**: Validates basic structure (Country Code + Checksum + BBAN) and ensures it stays within the 5-34 character range.
3.  **Rearrangement**: Moves the country code and checksum (first 4 characters) to the end of the string.
4.  **Expansion**: Replaces letters with their numeric values (A=10, ..., Z=35).
5.  **Modulo-97**: Uses `System.Numerics.BigInteger` to handle the resulting very large number and checks if `number % 97 == 1`.

The current code in `PaymentService.cs` already uses `BigInteger.Parse(numericString) % 97 == 1`, which is the standard way to implement this in C#. Reference to `System.Numerics` is already present at the top of the file.
```

---

### T2 – Bericht aller Konten mit negativem Saldo

| Metrik | Variante A (Legacy) | Variante B (AI-Ready) | Delta |
|---|---|---|---|
| Q1 Qualität erster Vorschlag (1–5) | 5 | 5 | 0 |
| Q2 Anzahl Iterationen | 1 | 1 | 0 |
| Q3 Korrekte Domänenbegriffe (Anzahl) | 1 | 2 | 1 |
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
```
To implement the `GetNegativeBalanceReport()` method in `AccountService`, I'll first update the `IAccountService` interface and then add the implementation to `AccountService`.

### 1. Update `IAccountService.cs`

Add the method definition to the interface:
```

```csharp
// /Users/alphazero/projects/hslu/msed/transferarbeit/poc/variant-b/Account/IAccountService.cs

public interface IAccountService
{
    // ... existing methods ...

    /// <summary>
    /// Erstellt einen formatierten Bericht über alle Konten mit negativem Saldo.
    /// </summary>
    /// <returns>Ein string-basierter Bericht für das Risikomanagement.</returns>
    string GetNegativeBalanceReport();
}
```

```
### 2. Update `AccountService.cs`

Implement the method using the existing `GetAccountsWithNegativeBalance()`:
```

```csharp
// /Users/alphazero/projects/hslu/msed/transferarbeit/poc/variant-b/Account/AccountService.cs

using System.Text; // Ensure System.Text is available for StringBuilder

public class AccountService : IAccountService
{
    // ... existing code ...

    /// <inheritdoc/>
    public string GetNegativeBalanceReport()
    {
        var negativeAccounts = GetAccountsWithNegativeBalance();
        
        if (negativeAccounts.Count == 0)
        {
            return "No accounts with negative balance found.";
        }

        var report = new StringBuilder();
        report.AppendLine("Negative Balance Report");
        report.AppendLine("=======================");
        report.AppendLine($"Generated at: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine();

        foreach (var account in negativeAccounts)
        {
            report.AppendLine($"IBAN: {account.Iban} | Balance: {account.Balance.Amount:F2} {account.Balance.Currency} | Status: {account.Status}");
        }

        report.AppendLine();
        report.AppendLine($"Total accounts: {negativeAccounts.Count}");

        return report.ToString();
    }
}
```

```
### Summary of Changes:
- **`IAccountService.cs`**: Added the method signature to the interface.
- **`AccountService.cs`**: Implemented `GetNegativeBalanceReport()` which calls `GetAccountsWithNegativeBalance()`, iterates through the results, and formats them into a readable string using `StringBuilder`.
```

Persönliche Notiz:

Hier ist die Ausgabe der Methode:

```
Negative Balance Report
=======================
Generated at: 2026-03-14 13:54:45 UTC

IBAN: CH5604835012345678010 | Balance: -200.50 CHF | Status: Active
IBAN: CH7204835012345678012 | Balance: -1050.75 CHF | Status: Blocked

Total accounts: 2
```

---

### T3 – Konto sperren mit Audit-Log

| Metrik | Variante A (Legacy) | Variante B (AI-Ready) | Delta |
|---|---|---|---|
| Q1 Qualität erster Vorschlag (1–5) | 3 | 5 | 2 |
| Q2 Anzahl Iterationen | 2 | 1 | 1 |
| Q3 Korrekte Domänenbegriffe (Anzahl) | 0 | 3 | 3 |
| Notizen / Beobachtungen | mehrere iterationen nötig | kein change nötig und so deklariert. Für Q3: es wurden viel mehr treffer gefunden | |

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
```
I have reviewed the `AccountService.cs` file. The `BlockAccount` method in its current state already implements the requirement to append an `AccountAuditEntry` after blocking an account.

Here is the relevant section of the code in `/Users/alphazero/projects/hslu/msed/transferarbeit/poc/variant-b/Account/AccountService.cs`:
```

```csharp
// LINE 58 | // Zustand-Mutation über Record-with
// LINE 59 | var blockedAccount = account with { Status = AccountStatus.Blocked };
// LINE 60 | _repository.Save(blockedAccount);
// LINE 61 | 
// LINE 62 | // Audit-Pflicht: jede Sperrung wird unveränderlich protokolliert
// LINE 63 | _repository.AppendAuditEntry(new AccountAuditEntry(
// LINE 64 |     EntryId: Guid.NewGuid(),
// LINE 65 |     Iban: iban,
// LINE 66 |     EventType: "ACCOUNT_BLOCKED",
// LINE 67 |     Description: $"Konto gesperrt. Grund: {reason}",
// LINE 68 |     InitiatedBy: initiatedBy,
// LINE 69 |     OccurredAt: DateTimeOffset.UtcNow
// LINE 70 | ));
```

```
### Analysis:
1.  **State Mutation**: At line 59, the account status is changed to `Blocked`.
2.  **Persistence**: At line 60, the updated account is saved to the repository.
3.  **Auditing**: At lines 63–70, an `AccountAuditEntry` is created and appended via `_repository.AppendAuditEntry`.

The implementation follows the requirement: it ensures that every successful blocking operation is followed by an immutable audit record containing the reason, the initiator, and the timestamp. No further changes are needed to satisfy the request.
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
