// ================================================================
// BankSystemT4Tests.cs – Variante A: T4 Ausgehende Überweisung
//
// Testszenarien für InitiateOutgoingTransfer.
// Stubs werfen NotImplementedException – dienen als Scaffold
// für das Copilot-Experiment (T4).
// Nach Copilot-Implementierung sollen alle 7 Tests grün sein.
// ================================================================

public class BankSystemT4Tests
{
    private BankSystem CreateSystem()
    {
        var sys = new BankSystem();
        sys.Run();
        return sys;
    }

    // Konto 1001: IBAN CH5604835012345678009, balance=1500, limit=5000, ACTIVE
    // Konto 1004: IBAN CH7204835012345678012, balance=-1050.75, BLOCKED
    // Konto 1006: IBAN CH5604835012345678014, balance=12300, limit=10000, ACTIVE
    // Ziel-IBAN (extern, gültig): DE88500700100175526303

    [Fact]
    public void T4_ValidTransfer_ReturnsTrue()
    {
        var sys = CreateSystem();
        bool result = sys.InitiateOutgoingTransfer(
            sourceAccountId: 1001,
            destinationIban: "DE88500700100175526303",
            amount: 100m,
            description: "Miete",
            initiatorId: "user-42");
        Assert.True(result);
    }

    [Fact]
    public void T4_InsufficientBalance_ReturnsFalse()
    {
        var sys = CreateSystem();
        // Konto 1001 hat 1500 CHF, 2000 übersteigt Saldo
        bool result = sys.InitiateOutgoingTransfer(
            sourceAccountId: 1001,
            destinationIban: "DE88500700100175526303",
            amount: 2000m,
            description: "Zu viel",
            initiatorId: "user-42");
        Assert.False(result);
    }

    [Fact]
    public void T4_ExceedsDailyLimit_ReturnsFalse()
    {
        var sys = CreateSystem();
        // Konto 1006 hat limit=10000, Betrag 10001 überschreitet Limit
        bool result = sys.InitiateOutgoingTransfer(
            sourceAccountId: 1006,
            destinationIban: "DE88500700100175526303",
            amount: 10001m,
            description: "Limit-Test",
            initiatorId: "user-42");
        Assert.False(result);
    }

    [Fact]
    public void T4_BlockedSourceAccount_ReturnsFalse()
    {
        var sys = CreateSystem();
        // Konto 1004 ist BLOCKED
        bool result = sys.InitiateOutgoingTransfer(
            sourceAccountId: 1004,
            destinationIban: "DE88500700100175526303",
            amount: 10m,
            description: "Gesperrtes Konto",
            initiatorId: "user-42");
        Assert.False(result);
    }

    [Fact]
    public void T4_UnknownSourceAccount_ReturnsFalse()
    {
        var sys = CreateSystem();
        bool result = sys.InitiateOutgoingTransfer(
            sourceAccountId: 9999,
            destinationIban: "DE88500700100175526303",
            amount: 10m,
            description: "Unbekanntes Konto",
            initiatorId: "user-42");
        Assert.False(result);
    }

    [Fact]
    public void T4_FeeDeducted_BalanceIsAmountPlusFiftyRappen()
    {
        var sys = CreateSystem();
        decimal before = sys.GetBalance(1001);
        sys.InitiateOutgoingTransfer(
            sourceAccountId: 1001,
            destinationIban: "DE88500700100175526303",
            amount: 100m,
            description: "Fee-Test",
            initiatorId: "user-42");
        decimal after = sys.GetBalance(1001);
        Assert.Equal(before - 100m - 0.50m, after);
    }

    [Fact]
    public void T4_SuccessfulTransfer_AuditLogEntryWritten()
    {
        var sys = CreateSystem();
        int logBefore = sys.GetAuditLogCount();
        sys.InitiateOutgoingTransfer(
            sourceAccountId: 1001,
            destinationIban: "DE88500700100175526303",
            amount: 50m,
            description: "Audit-Test",
            initiatorId: "user-42");
        int logAfter = sys.GetAuditLogCount();
        Assert.True(logAfter > logBefore);
    }
}
