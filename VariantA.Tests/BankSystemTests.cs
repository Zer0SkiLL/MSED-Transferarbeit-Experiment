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
        bool result = sys.BlockAccount(1001, "Fraud");
        Assert.True(result);
    }

    [Fact]
    public void T3_BlockAccount_SetsStatusToBlocked()
    {
        var sys = CreateSystem();
        sys.BlockAccount(1001, "Fraud");
        string status = sys.GetAccountStatus(1001);
        Assert.Equal("BLOCKED", status);
    }

    [Fact]
    public void T3_BlockAccount_AlreadyBlockedAccount_ReturnsFalse()
    {
        var sys = CreateSystem();
        // Konto 1004 ist bereits BLOCKED
        bool result = sys.BlockAccount(1004, "Nochmals");
        Assert.False(result);
    }

    [Fact]
    public void T3_BlockAccount_UnknownAccount_ReturnsFalse()
    {
        var sys = CreateSystem();
        bool result = sys.BlockAccount(9999, "Test");
        Assert.False(result);
    }

    [Fact]
    public void T3_BlockAccount_WritesAuditLogEntry()
    {
        var sys = CreateSystem();
        int logCountBefore = sys.GetAuditLog().Count;
        sys.BlockAccount(1001, "SuspectedFraud");
        int logCountAfter = sys.GetAuditLog().Count;
        Assert.True(logCountAfter > logCountBefore);
    }

    [Fact]
    public void T3_BlockAccount_AuditLogEntryContainsAccountId()
    {
        var sys = CreateSystem();
        sys.BlockAccount(1006, "RegulatoryOrder");
        var log = sys.GetAuditLogByCategory("BLOCK");
        Assert.Contains(log, e => e[2].ToString()!.Contains("1006"));
    }

    [Fact]
    public void T3_BlockedAccount_CannotProcessPayment()
    {
        var sys = CreateSystem();
        sys.BlockAccount(1001, "Fraud");
        bool payment = sys.ProcessPayment(1001, "CH5604835012345678011", 10m, "Test");
        Assert.False(payment);
    }
}
