// ================================================================
// PaymentServiceTests.cs – Variante B: Tests für Payment-Context
// T1: IBAN-Validierung (ISO 13616 Mod-97) + Zahlungseingang
// ================================================================

using BankApp.Account;
using BankApp.Payment;

public class PaymentServiceTests
{
    private (IPaymentService service, InMemoryAccountRepository repo) CreateService()
    {
        var repo = new InMemoryAccountRepository();
        var service = new PaymentService(repo);
        return (service, repo);
    }

    // ----------------------------------------------------------------
    // T1 – IBAN-Validierung (ISO 13616 Mod-97)
    // ----------------------------------------------------------------

    [Theory]
    [InlineData("DE88500700100175526303")]   // Deutschland – korrekte Prüfziffer
    [InlineData("CH5604835012345678009")]    // Schweiz – Testdaten-IBAN
    [InlineData("AT611904300234573201")]     // Österreich
    [InlineData("GB29NWBK60161331926819")]   // Grossbritannien
    public void T1_ValidateIban_ValidIbans_ReturnTrue(string iban)
    {
        var (service, _) = CreateService();
        Assert.True(service.ValidateIban(iban));
    }

    [Theory]
    [InlineData("DE89370400440532013001")]   // falsche Prüfziffer (letzte Ziffer geändert)
    [InlineData("CH123")]                   // zu kurz
    [InlineData("")]                        // leer
    [InlineData("XYZABC")]                  // kein gültiges Format
    [InlineData("12DE89370400440532013000")] // Ländercode nicht vorne
    public void T1_ValidateIban_InvalidIbans_ReturnFalse(string iban)
    {
        var (service, _) = CreateService();
        Assert.False(service.ValidateIban(iban));
    }

    [Fact]
    public void T1_ValidateIban_NullInput_ReturnsFalse()
    {
        var (service, _) = CreateService();
        Assert.False(service.ValidateIban(null!));
    }

    [Fact]
    public void T1_ValidateIban_WithSpaces_StillValidates()
    {
        var (service, _) = CreateService();
        // IBANs mit Leerzeichen (wie auf Kontoauszügen) müssen akzeptiert werden
        Assert.True(service.ValidateIban("DE89 3704 0044 0532 0130 00"));
    }

    [Fact]
    public void T1_ValidateIban_WrongLengthForCountry_ReturnsFalse()
    {
        var (service, _) = CreateService();
        // CH-IBAN muss 21 Zeichen haben – hier 20
        Assert.False(service.ValidateIban("CH560483501234567800"));
    }

    // ----------------------------------------------------------------
    // T1 – Eingehender Zahlungsauftrag (ReceiveIncomingPayment)
    // ----------------------------------------------------------------

    [Fact]
    public void T1_ReceiveIncomingPayment_ValidPayment_Succeeds()
    {
        var (service, _) = CreateService();
        var payment = new IncomingPayment(
            DebtorIban: "DE89370400440532013000",
            CreditorIban: "CH5604835012345678009",
            Amount: new Money(500m, Currency.CHF),
            RemittanceInfo: "Gutschrift Auftrag 4711");
        var result = service.ReceiveIncomingPayment(payment);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.PaymentId);
    }

    [Fact]
    public void T1_ReceiveIncomingPayment_CreditsAccountBalance()
    {
        var (service, repo) = CreateService();
        decimal balanceBefore = repo.FindByIban("CH5604835012345678009")!.Balance.Amount;
        service.ReceiveIncomingPayment(new IncomingPayment(
            "DE89370400440532013000",
            "CH5604835012345678009",
            new Money(300m, Currency.CHF),
            "Test"));
        decimal balanceAfter = repo.FindByIban("CH5604835012345678009")!.Balance.Amount;
        Assert.Equal(balanceBefore + 300m, balanceAfter);
    }

    [Fact]
    public void T1_ReceiveIncomingPayment_InvalidCreditorIban_Fails()
    {
        var (service, _) = CreateService();
        var result = service.ReceiveIncomingPayment(new IncomingPayment(
            "DE89370400440532013000",
            "INVALID_IBAN",
            new Money(100m, Currency.CHF),
            "Test"));
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDITOR_IBAN", result.ErrorCode);
    }

    [Fact]
    public void T1_ReceiveIncomingPayment_CreditorAccountNotFound_Fails()
    {
        var (service, _) = CreateService();
        // Gültige IBAN-Struktur, aber nicht im System
        var result = service.ReceiveIncomingPayment(new IncomingPayment(
            "DE89370400440532013000",
            "CH9804835012345678999",  // gültige IBAN-Prüfziffer, aber nicht im System
            new Money(100m, Currency.CHF),
            "Test"));
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void T1_ReceiveIncomingPayment_ToBlockedAccount_Fails()
    {
        var (service, _) = CreateService();
        // CH...012 ist BLOCKED im Testdatensatz (CH72... = korrekte Prüfziffer)
        var result = service.ReceiveIncomingPayment(new IncomingPayment(
            "DE89370400440532013000",
            "CH7204835012345678012",
            new Money(100m, Currency.CHF),
            "Test"));
        Assert.False(result.IsSuccess);
        Assert.Equal("CREDITOR_ACCOUNT_NOT_ACTIVE", result.ErrorCode);
    }

    [Fact]
    public void T1_ReceiveIncomingPayment_ZeroAmount_Fails()
    {
        var (service, _) = CreateService();
        var result = service.ReceiveIncomingPayment(new IncomingPayment(
            "DE89370400440532013000",
            "CH5604835012345678009",
            new Money(0m, Currency.CHF),
            "Test"));
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_AMOUNT", result.ErrorCode);
    }
}
