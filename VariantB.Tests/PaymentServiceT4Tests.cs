// ================================================================
// PaymentServiceT4Tests.cs – Variante B: T4 Ausgehende Überweisung
//
// Testszenarien für InitiateOutgoingTransfer (PaymentService).
// Stubs werfen NotImplementedException – dienen als Scaffold
// für das Copilot-Experiment (T4).
// Nach Copilot-Implementierung sollen alle 6 Tests grün sein.
//
// Hinweis: Kein Daily-Limit-Test, da InMemoryAccountRepository
// kein Limit-Konzept enthält (bewusste Architektur-Limitation).
// ================================================================

using BankApp.Account;
using BankApp.Payment;

public class PaymentServiceT4Tests
{
    private (IPaymentService service, InMemoryAccountRepository repo) CreateService()
    {
        var repo = new InMemoryAccountRepository();
        var service = new PaymentService(repo);
        return (service, repo);
    }

    // Testdaten aus InMemoryAccountRepository:
    // CH5604835012345678009: balance=1500 CHF, Active
    // CH7204835012345678012: balance=-1050.75 CHF, Blocked
    // Ziel-IBAN (extern, gültig): DE88500700100175526303

    [Fact]
    public void T4_ValidTransfer_ReturnsSuccess()
    {
        var (service, _) = CreateService();
        var transfer = new OutgoingTransfer(
            DebtorIban: "CH5604835012345678009",
            CreditorIban: "DE88500700100175526303",
            Amount: new Money(100m, Currency.CHF),
            RemittanceInfo: "Miete",
            InitiatedBy: "user-42");

        var result = service.InitiateOutgoingTransfer(transfer);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void T4_InsufficientBalance_ReturnsFail()
    {
        var (service, _) = CreateService();
        // Balance ist 1500, 2000 übersteigt Saldo
        var transfer = new OutgoingTransfer(
            DebtorIban: "CH5604835012345678009",
            CreditorIban: "DE88500700100175526303",
            Amount: new Money(2000m, Currency.CHF),
            RemittanceInfo: "Zu viel",
            InitiatedBy: "user-42");

        var result = service.InitiateOutgoingTransfer(transfer);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void T4_BlockedSourceAccount_ReturnsFail()
    {
        var (service, _) = CreateService();
        // CH7204835012345678012 ist Blocked
        var transfer = new OutgoingTransfer(
            DebtorIban: "CH7204835012345678012",
            CreditorIban: "DE88500700100175526303",
            Amount: new Money(10m, Currency.CHF),
            RemittanceInfo: "Gesperrtes Konto",
            InitiatedBy: "user-42");

        var result = service.InitiateOutgoingTransfer(transfer);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void T4_UnknownSourceAccount_ReturnsFail()
    {
        var (service, _) = CreateService();
        var transfer = new OutgoingTransfer(
            DebtorIban: "CH0000000000000000000",
            CreditorIban: "DE88500700100175526303",
            Amount: new Money(10m, Currency.CHF),
            RemittanceInfo: "Unbekanntes Konto",
            InitiatedBy: "user-42");

        var result = service.InitiateOutgoingTransfer(transfer);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void T4_FeeDeducted_BalanceIsAmountPlusFiftyRappen()
    {
        var (service, repo) = CreateService();
        var accountBefore = repo.FindByIban("CH5604835012345678009")!;
        decimal balanceBefore = accountBefore.Balance.Amount;

        var transfer = new OutgoingTransfer(
            DebtorIban: "CH5604835012345678009",
            CreditorIban: "DE88500700100175526303",
            Amount: new Money(100m, Currency.CHF),
            RemittanceInfo: "Fee-Test",
            InitiatedBy: "user-42");

        service.InitiateOutgoingTransfer(transfer);

        var accountAfter = repo.FindByIban("CH5604835012345678009")!;
        Assert.Equal(balanceBefore - 100m - 0.50m, accountAfter.Balance.Amount);
    }

    [Fact]
    public void T4_SuccessfulTransfer_AuditLogEntryWritten()
    {
        var (service, repo) = CreateService();
        int logBefore = repo.GetAuditLog().Count;

        var transfer = new OutgoingTransfer(
            DebtorIban: "CH5604835012345678009",
            CreditorIban: "DE88500700100175526303",
            Amount: new Money(50m, Currency.CHF),
            RemittanceInfo: "Audit-Test",
            InitiatedBy: "user-42");

        service.InitiateOutgoingTransfer(transfer);

        int logAfter = repo.GetAuditLog().Count;
        Assert.True(logAfter > logBefore);
    }
}
