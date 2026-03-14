namespace BankApp.Account;

// ============================================================
// Domain Models – Bounded Context: Account
// Ubiquitous Language der Kernbanken-Domäne
// ============================================================

/// <summary>
/// Eindeutiger Bezeichner eines Bankkontos, dargestellt als IBAN.
/// </summary>
public record AccountId(string Iban);

/// <summary>
/// Währungsbetrag mit expliziter Währungsangabe.
/// Verhindert implizite Vermischung von CHF und EUR.
/// </summary>
public record Money(decimal Amount, Currency Currency)
{
    public Money Add(Money other)
    {
        if (other.Currency != Currency)
            throw new InvalidOperationException($"Währungsmismatch: {Currency} vs {other.Currency}");
        return this with { Amount = Amount + other.Amount };
    }

    public Money Subtract(Money other)
    {
        if (other.Currency != Currency)
            throw new InvalidOperationException($"Währungsmismatch: {Currency} vs {other.Currency}");
        return this with { Amount = Amount - other.Amount };
    }

    public bool IsNegative => Amount < 0;
}

/// <summary>
/// Unterstützte Währungen gemäss ISO 4217.
/// </summary>
public enum Currency { CHF, EUR, USD }

/// <summary>
/// Status eines Bankkontos im Lebenszyklus.
/// </summary>
public enum AccountStatus
{
    /// <summary>Konto ist aktiv und für Transaktionen freigegeben.</summary>
    Active,
    /// <summary>Konto ist gesperrt – keine Ein- oder Auszahlungen möglich.</summary>
    Blocked,
    /// <summary>Konto ist geschlossen – nur noch lesend zugreifbar.</summary>
    Closed
}

/// <summary>
/// Grund für eine Kontosperrung (Audit-Pflicht).
/// </summary>
public enum BlockReason
{
    SuspectedFraud,
    CustomerRequest,
    RegulatoryOrder,
    InactivityThresholdExceeded
}

/// <summary>
/// Unveränderliches Werteobjekt: Repräsentiert ein Bankkonto
/// mit seinem aktuellen Saldo und Status.
/// </summary>
public record BankAccount(
    AccountId Id,
    string Iban,
    int CustomerId,
    AccountType Type,
    Money Balance,
    AccountStatus Status
)
{
    public bool IsActive => Status == AccountStatus.Active;
    public bool HasNegativeBalance => Balance.IsNegative;
}

/// <summary>
/// Kontotyp gemäss Produktkatalog der Bank.
/// </summary>
public enum AccountType { Girokonto, Sparkonto, Festgeldkonto }

/// <summary>
/// Audit-Log-Eintrag für sicherheitsrelevante Kontooperationen.
/// Unveränderlich – einmal geschrieben, nie geändert.
/// </summary>
public record AccountAuditEntry(
    Guid EntryId,
    string Iban,
    string EventType,
    string Description,
    string InitiatedBy,
    DateTimeOffset OccurredAt
);

/// <summary>
/// Rückgabewert für Account-Operationen mit strukturierter Fehlerbehandlung.
/// </summary>
public record AccountOperationResult(bool IsSuccess, string? ErrorCode = null)
{
    public static AccountOperationResult Ok() => new(true);
    public static AccountOperationResult Fail(string errorCode) => new(false, errorCode);
}
