namespace BankApp.Account;

// ============================================================
// IAccountRepository – Bounded Context: Account
// Contract-First Interface (M1 + M2)
// ============================================================

/// <summary>
/// Repository-Abstraktion für den Zugriff auf Bankkonten.
/// Implementierungen können gegen SQL, Core-Banking-API oder
/// In-Memory ausgetauscht werden ohne Änderung an Services.
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Gibt das Konto zur angegebenen IBAN zurück, oder null falls nicht gefunden.
    /// </summary>
    BankAccount? FindByIban(string iban);

    /// <summary>
    /// Gibt alle Konten zurück, deren aktueller Saldo negativ ist.
    /// </summary>
    IReadOnlyList<BankAccount> FindAllWithNegativeBalance();

    /// <summary>
    /// Persistiert den aktuellen Zustand eines Kontos.
    /// </summary>
    void Save(BankAccount account);

    /// <summary>
    /// Schreibt einen Audit-Eintrag für eine sicherheitsrelevante Operation.
    /// </summary>
    void AppendAuditEntry(AccountAuditEntry entry);
}

/// <summary>
/// In-Memory-Implementierung für PoC und Tests.
/// </summary>
public class InMemoryAccountRepository : IAccountRepository
{
    private readonly Dictionary<string, BankAccount> _accounts = new();
    private readonly List<AccountAuditEntry> _auditLog = new();

    public InMemoryAccountRepository()
    {
        // Testdaten mit realistischer Domänensprache
        var accounts = new[]
        {
            new BankAccount(new AccountId("CH5604835012345678009"), "CH5604835012345678009", 1,
                AccountType.Girokonto, new Money(1500.00m, Currency.CHF), AccountStatus.Active),
            new BankAccount(new AccountId("CH5604835012345678010"), "CH5604835012345678010", 1,
                AccountType.Sparkonto,  new Money(-200.50m, Currency.CHF), AccountStatus.Active),
            new BankAccount(new AccountId("CH5604835012345678011"), "CH5604835012345678011", 2,
                AccountType.Girokonto, new Money(800.00m,  Currency.CHF), AccountStatus.Active),
            new BankAccount(new AccountId("CH7204835012345678012"), "CH7204835012345678012", 2,
                AccountType.Sparkonto,  new Money(-1050.75m,Currency.CHF), AccountStatus.Blocked),
            new BankAccount(new AccountId("CH5604835012345678013"), "CH5604835012345678013", 3,
                AccountType.Girokonto, new Money(0.00m,    Currency.CHF), AccountStatus.Active),
        };
        foreach (var a in accounts) _accounts[a.Iban] = a;
    }

    public BankAccount? FindByIban(string iban) =>
        _accounts.TryGetValue(iban, out var acc) ? acc : null;

    public IReadOnlyList<BankAccount> FindAllWithNegativeBalance() =>
        _accounts.Values.Where(a => a.HasNegativeBalance).ToList();

    public void Save(BankAccount account) =>
        _accounts[account.Iban] = account;

    public void AppendAuditEntry(AccountAuditEntry entry) =>
        _auditLog.Add(entry);

    public IReadOnlyList<AccountAuditEntry> GetAuditLog() => _auditLog.AsReadOnly();
}
