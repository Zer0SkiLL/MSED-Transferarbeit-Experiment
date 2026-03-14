// ================================================================
// BankSystemTests.cs – Variante A: Funktionstests God-Class
//
// Testet die drei PoC-Tasks (T1/T2/T3) direkt auf BankSystem.
// Dient als Smoke-Test vor und nach dem Copilot-Experiment,
// um sicherzustellen dass die Codebasis lauffähig ist.
// ================================================================

public class BankSystemTests
{
    // Hilfsmethode: frische Instanz mit Testdaten
    private BankSystem CreateSystem()
    {
        var sys = new BankSystem();
        sys.Run(); // lädt Testdaten via Init()
        return sys;
    }

    // ----------------------------------------------------------------
    // T1 – Zahlungsverarbeitung / IBAN-Validierung
    // ----------------------------------------------------------------

    [Fact]
    public void T1_ProcessPayment_ValidIban_SufficientFunds_ReturnsTrue()
    {
        var sys = CreateSystem();
        // Konto 1001 hat 1500 CHF, Limit 5000
        bool result = sys.ProcessPayment(1001, "DE88500700100175526303", 100m, "Test-Überweisung");
        Assert.True(result);
    }

    [Fact]
    public void T1_ProcessPayment_DeductsAmountAndFeeFromBalance()
    {
        var sys = CreateSystem();
        decimal before = sys.GetBalance(1001);
        sys.ProcessPayment(1001, "DE88 5007 0010 0175 5263 03", 100m, "Test");
        decimal after = sys.GetBalance(1001);
        // Betrag + Gebühr (0.50 bei <= 1000)
        Assert.Equal(before - 100m - 0.50m, after);
    }

    [Fact]
    public void T1_ProcessPayment_IbanTooShort_ReturnsFalse()
    {
        var sys = CreateSystem();
        bool result = sys.ProcessPayment(1001, "CH123", 100m, "Test");
        Assert.False(result);
    }

    [Fact]
    public void T1_ProcessPayment_NullIban_ReturnsFalse()
    {
        var sys = CreateSystem();
        bool result = sys.ProcessPayment(1001, null!, 100m, "Test");
        Assert.False(result);
    }

    [Fact]
    public void T1_ProcessPayment_InsufficientFunds_ReturnsFalse()
    {
        var sys = CreateSystem();
        // Konto 1001 hat 1500 CHF – Betrag zu gross
        bool result = sys.ProcessPayment(1001, "CH5604835012345678011", 99999m, "Test");
        Assert.False(result);
    }

    [Fact]
    public void T1_ProcessPayment_BlockedSourceAccount_ReturnsFalse()
    {
        var sys = CreateSystem();
        // Konto 1004 ist BLOCKED
        bool result = sys.ProcessPayment(1004, "CH5604835012345678009", 50m, "Test");
        Assert.False(result);
    }

    [Fact]
    public void T1_ProcessPayment_UnknownAccount_ReturnsFalse()
    {
        var sys = CreateSystem();
        bool result = sys.ProcessPayment(9999, "CH5604835012345678009", 50m, "Test");
        Assert.False(result);
    }

    // ----------------------------------------------------------------
    // T2 – Negativsaldo-Report
    // ----------------------------------------------------------------

    [Fact]
    public void T2_GetNegativeBalanceAccounts_ReturnsCorrectCount()
    {
        var sys = CreateSystem();
        // Testdaten: 1002 (-200.50), 1004 (-1050.75), 1008 (-88.30) = 3 Konten
        var result = sys.GetNegativeBalanceAccounts();
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void T2_GetNegativeBalanceAccounts_AllReturnedBalancesAreNegative()
    {
        var sys = CreateSystem();
        var result = sys.GetNegativeBalanceAccounts();
        Assert.All(result, row => Assert.True((decimal)row[3] < 0));
    }

    [Fact]
    public void T2_GetNegativeBalanceAccounts_ContainsExpectedAccountIds()
    {
        var sys = CreateSystem();
        var result = sys.GetNegativeBalanceAccounts();
        var ids = result.Select(r => (int)r[0]).ToList();
        Assert.Contains(1002, ids);
        Assert.Contains(1004, ids);
        Assert.Contains(1008, ids);
    }

    [Fact]
    public void T2_GetOverdrawnAccountIds_MatchesNegativeBalanceAccounts()
    {
        var sys = CreateSystem();
        var fromNegative = sys.GetNegativeBalanceAccounts().Select(r => (int)r[0]).OrderBy(x => x).ToList();
        var fromOverdrawn = sys.GetOverdrawnAccountIds().OrderBy(x => x).ToList();
        Assert.Equal(fromNegative, fromOverdrawn);
    }
    
    [Fact]
    public void T2_GenerateNegativeBalanceReport_EvalOutput() 
    {
        var sys = CreateSystem();
        var result = sys.GenerateNegativeBalanceReport();
        Assert.Contains("=== BERICHT: KONTEN MIT NEGATIVEM SALDO ===", result);
        // TODO: assert logic
    }

    // ----------------------------------------------------------------
    // T3 – Kontosperrung mit Audit-Log
    // ----------------------------------------------------------------

    [Fact]
    public void T3_BlockAccount_ActiveAccount_ReturnsTrue()
    {
        var sys = CreateSystem();
        bool result = sys.BlockAccount(1001, "Fraud", "Test");
        Assert.True(result);
    }

    [Fact]
    public void T3_BlockAccount_SetsStatusToBlocked()
    {
        var sys = CreateSystem();
        sys.BlockAccount(1001, "Fraud", "Test");
        string status = sys.GetAccountStatus(1001);
        Assert.Equal("BLOCKED", status);
    }

    [Fact]
    public void T3_BlockAccount_AlreadyBlockedAccount_ReturnsFalse()
    {
        var sys = CreateSystem();
        // Konto 1004 ist bereits BLOCKED
        bool result = sys.BlockAccount(1004, "Nochmals", "Test");
        Assert.False(result);
    }

    [Fact]
    public void T3_BlockAccount_UnknownAccount_ReturnsFalse()
    {
        var sys = CreateSystem();
        bool result = sys.BlockAccount(9999, "Test", "Test");
        Assert.False(result);
    }

    [Fact]
    public void T3_BlockAccount_WritesAuditLogEntry()
    {
        var sys = CreateSystem();
        int logCountBefore = sys.GetAuditLog().Count;
        sys.BlockAccount(1001, "SuspectedFraud", "Test");
        int logCountAfter = sys.GetAuditLog().Count;
        Assert.True(logCountAfter > logCountBefore);
    }

    [Fact]
    public void T3_BlockAccount_AuditLogEntryContainsAccountId()
    {
        var sys = CreateSystem();
        sys.BlockAccount(1006, "RegulatoryOrder", "Test");
        var log = sys.GetAuditLogByCategory("BLOCK");
        Assert.Contains(log, e => e[2].ToString()!.Contains("1006"));
    }

    [Fact]
    public void T3_BlockedAccount_CannotProcessPayment()
    {
        var sys = CreateSystem();
        sys.BlockAccount(1001, "Fraud", "Test");
        bool payment = sys.ProcessPayment(1001, "CH5604835012345678011", 10m, "Test");
        Assert.False(payment);
    }
    
    // ----------------------------------------------------------------
    // T4 - InitiateOutgoingTransfer
    // ----------------------------------------------------------------
    
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
