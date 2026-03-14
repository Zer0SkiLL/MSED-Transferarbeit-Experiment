// ================================================================
// AccountServiceTests.cs – Variante B: Tests für Account-Context
// ================================================================

using BankApp.Account;

public class AccountServiceTests
{
    private (IAccountService service, InMemoryAccountRepository repo) CreateService()
    {
        var repo = new InMemoryAccountRepository();
        var service = new AccountService(repo);
        return (service, repo);
    }

    // ----------------------------------------------------------------
    // T2 – Negativsaldo-Report
    // ----------------------------------------------------------------

    [Fact]
    public void T2_GetAccountsWithNegativeBalance_ReturnsCorrectCount()
    {
        var (service, _) = CreateService();
        // Testdaten: CH...010 (-200.50), CH...012 (-1050.75) = 2 Konten
        var result = service.GetAccountsWithNegativeBalance();
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void T2_GetAccountsWithNegativeBalance_AllBalancesAreNegative()
    {
        var (service, _) = CreateService();
        var result = service.GetAccountsWithNegativeBalance();
        Assert.All(result, acc => Assert.True(acc.Balance.IsNegative));
    }

    [Fact]
    public void T2_GetAccountsWithNegativeBalance_ContainsExpectedIbans()
    {
        var (service, _) = CreateService();
        var result = service.GetAccountsWithNegativeBalance();
        var ibans = result.Select(a => a.Iban).ToList();
        Assert.Contains("CH5604835012345678010", ibans);
        Assert.Contains("CH7204835012345678012", ibans);
    }

    [Fact]
    public void T2_GetNegativeBalanceReport_ContainsHeader()
    {
        var (service, _) = CreateService();
        var report = service.GetNegativeBalanceReport();
        Assert.Contains("Negative Balance Report", report);
        Assert.Contains("=======================", report);
    }

    [Fact]
    public void T2_GetNegativeBalanceReport_ContainsExpectedIbans()
    {
        var (service, _) = CreateService();
        var report = service.GetNegativeBalanceReport();
        Assert.Contains("CH5604835012345678010", report);
        Assert.Contains("CH7204835012345678012", report);
    }

    [Fact]
    public void T2_GetNegativeBalanceReport_ContainsTotalCount()
    {
        var (service, _) = CreateService();
        var report = service.GetNegativeBalanceReport();
        Assert.Contains("Total accounts: 2", report);
    }

    [Fact]
    public void T2_GetNegativeBalanceReport_NoNegativeAccounts_ReturnsEmptyMessage()
    {
        var repo = new InMemoryAccountRepository();
        foreach (var acc in repo.FindAllWithNegativeBalance())
        {
            var cleared = acc with { Balance = new Money(0m, acc.Balance.Currency) };
            repo.Save(cleared);
        }
        var service = new AccountService(repo);
        var report = service.GetNegativeBalanceReport();
        Assert.Equal("No accounts with negative balance found.", report);
    }

    // ----------------------------------------------------------------
    // T3 – Kontosperrung mit Audit-Log
    // ----------------------------------------------------------------

    [Fact]
    public void T3_BlockAccount_ActiveAccount_Succeeds()
    {
        var (service, _) = CreateService();
        var result = service.BlockAccount(
            "CH5604835012345678009",
            BlockReason.SuspectedFraud,
            "compliance@bank.ch");
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void T3_BlockAccount_SetsStatusToBlocked()
    {
        var (service, repo) = CreateService();
        service.BlockAccount("CH5604835012345678009", BlockReason.SuspectedFraud, "compliance@bank.ch");
        var account = repo.FindByIban("CH5604835012345678009");
        Assert.Equal(AccountStatus.Blocked, account!.Status);
    }

    [Fact]
    public void T3_BlockAccount_AlreadyBlocked_ReturnsError()
    {
        var (service, _) = CreateService();
        // CH...012 ist bereits Blocked im Testdatensatz
        var result = service.BlockAccount(
            "CH7204835012345678012",
            BlockReason.RegulatoryOrder,
            "admin@bank.ch");
        Assert.False(result.IsSuccess);
        Assert.Equal("ACCOUNT_ALREADY_BLOCKED", result.ErrorCode);
    }

    [Fact]
    public void T3_BlockAccount_UnknownIban_ReturnsNotFound()
    {
        var (service, _) = CreateService();
        var result = service.BlockAccount("CH0000000000000000000", BlockReason.CustomerRequest, "user");
        Assert.False(result.IsSuccess);
        Assert.Equal("ACCOUNT_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public void T3_BlockAccount_WritesAuditEntry()
    {
        var (service, repo) = CreateService();
        service.BlockAccount("CH5604835012345678009", BlockReason.SuspectedFraud, "compliance@bank.ch");
        var log = repo.GetAuditLog();
        Assert.Single(log);
    }

    [Fact]
    public void T3_BlockAccount_AuditEntry_HasCorrectEventType()
    {
        var (service, repo) = CreateService();
        service.BlockAccount("CH5604835012345678011", BlockReason.RegulatoryOrder, "admin@bank.ch");
        var entry = repo.GetAuditLog().Single();
        Assert.Equal("ACCOUNT_BLOCKED", entry.EventType);
    }

    [Fact]
    public void T3_BlockAccount_AuditEntry_ContainsInitiator()
    {
        var (service, repo) = CreateService();
        service.BlockAccount("CH5604835012345678009", BlockReason.SuspectedFraud, "officer-42@bank.ch");
        var entry = repo.GetAuditLog().Single();
        Assert.Equal("officer-42@bank.ch", entry.InitiatedBy);
    }

    [Fact]
    public void T3_CreditAccount_ActiveAccount_Succeeds()
    {
        var (service, repo) = CreateService();
        var result = service.CreditAccount(
            "CH5604835012345678009",
            new Money(500m, Currency.CHF),
            "Gutschrift Test");
        Assert.True(result.IsSuccess);
        var updated = repo.FindByIban("CH5604835012345678009");
        Assert.Equal(2000m, updated!.Balance.Amount);
    }

    [Fact]
    public void T3_CreditAccount_BlockedAccount_ReturnsNotActive()
    {
        var (service, _) = CreateService();
        var result = service.CreditAccount(
            "CH7204835012345678012",
            new Money(100m, Currency.CHF),
            "Test");
        Assert.False(result.IsSuccess);
        Assert.Equal("ACCOUNT_NOT_ACTIVE", result.ErrorCode);
    }
}
