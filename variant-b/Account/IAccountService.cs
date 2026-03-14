namespace BankApp.Account;

// ============================================================
// IAccountService – Bounded Context: Account
// Domänen-API (M1 Boundary-Schärfung)
// ============================================================

/// <summary>
/// Service-Interface für kontobezogene Geschäftsoperationen.
/// Kapselt alle Regeln rund um Kontoführung, Saldoverwaltung
/// und Compliance-Anforderungen (Sperrung, Audit).
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Gibt alle Konten zurück, deren aktueller Saldo negativ ist.
    /// Wird für das tägliche Negativsaldo-Reporting verwendet (Risikomanagement).
    /// </summary>
    IReadOnlyList<BankAccount> GetAccountsWithNegativeBalance();

    /// <summary>
    /// Sperrt ein Konto und schreibt einen Audit-Log-Eintrag.
    /// Eine Sperrung verhindert weitere Belastungen und Gutschriften.
    /// </summary>
    /// <param name="iban">IBAN des zu sperrenden Kontos.</param>
    /// <param name="reason">Fachlicher Grund der Sperrung (Pflichtfeld für Audit).</param>
    /// <param name="initiatedBy">Benutzer oder System, das die Sperrung auslöst.</param>
    AccountOperationResult BlockAccount(string iban, BlockReason reason, string initiatedBy);

    /// <summary>
    /// Schreibt eine Gutschrift auf ein Konto (z.B. bei eingehendem Zahlungsauftrag).
    /// </summary>
    AccountOperationResult CreditAccount(string iban, Money amount, string remittanceInfo);
}
