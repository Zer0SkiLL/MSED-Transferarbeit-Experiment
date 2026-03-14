using System.Text;

namespace BankApp.Account;

// ============================================================
// AccountService – Bounded Context: Account
// Implementierung der Geschäftslogik
// ============================================================

/// <summary>
/// Implementiert <see cref="IAccountService"/>.
/// Alle Mutationen laufen über den Service – kein direkter
/// Repository-Zugriff von aussen (M1 Boundary-Schärfung).
/// </summary>
public class AccountService : IAccountService
{
    private readonly IAccountRepository _repository;

    /// <summary>
    /// Erstellt den AccountService mit Abhängigkeit auf das Repository.
    /// Die Abhängigkeit ist explizit und austauschbar (Testbarkeit).
    /// </summary>
    public AccountService(IAccountRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public IReadOnlyList<BankAccount> GetAccountsWithNegativeBalance()
    {
        return _repository.FindAllWithNegativeBalance();
    }

    /// <inheritdoc/>
    public AccountOperationResult BlockAccount(string iban, BlockReason reason, string initiatedBy)
    {
        var account = _repository.FindByIban(iban);
        if (account is null)
            return AccountOperationResult.Fail("ACCOUNT_NOT_FOUND");

        if (account.Status == AccountStatus.Blocked)
            return AccountOperationResult.Fail("ACCOUNT_ALREADY_BLOCKED");

        if (account.Status == AccountStatus.Closed)
            return AccountOperationResult.Fail("ACCOUNT_CLOSED");

        // Zustand-Mutation über Record-with
        var blockedAccount = account with { Status = AccountStatus.Blocked };
        _repository.Save(blockedAccount);

        // Audit-Pflicht: jede Sperrung wird unveränderlich protokolliert
        _repository.AppendAuditEntry(new AccountAuditEntry(
            EntryId: Guid.NewGuid(),
            Iban: iban,
            EventType: "ACCOUNT_BLOCKED",
            Description: $"Konto gesperrt. Grund: {reason}",
            InitiatedBy: initiatedBy,
            OccurredAt: DateTimeOffset.UtcNow
        ));

        return AccountOperationResult.Ok();
    }

    /// <inheritdoc/>
    public AccountOperationResult CreditAccount(string iban, Money amount, string remittanceInfo)
    {
        var account = _repository.FindByIban(iban);
        if (account is null)
            return AccountOperationResult.Fail("ACCOUNT_NOT_FOUND");

        if (!account.IsActive)
            return AccountOperationResult.Fail("ACCOUNT_NOT_ACTIVE");

        var credited = account with { Balance = account.Balance.Add(amount) };
        _repository.Save(credited);

        _repository.AppendAuditEntry(new AccountAuditEntry(
            EntryId: Guid.NewGuid(),
            Iban: iban,
            EventType: "ACCOUNT_CREDITED",
            Description: $"Gutschrift {amount.Amount:F2} {amount.Currency}. Verwendungszweck: {remittanceInfo}",
            InitiatedBy: "payment-service",
            OccurredAt: DateTimeOffset.UtcNow
        ));

        return AccountOperationResult.Ok();
    }
    
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
