namespace BankApp.Payment;

using System.Numerics;
using System.Text.RegularExpressions;
using BankApp.Account;

// ============================================================
// PaymentService – Bounded Context: Payment
// IBAN-Validierung nach ISO 13616 (Mod-97-Algorithmus)
// ============================================================

/// <summary>
/// Implementiert <see cref="IPaymentService"/>.
/// Vollständige IBAN-Validierung gemäss ISO 13616-1:2007
/// inkl. Mod-97-Prüfzifferberechnung.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IAccountRepository _accountRepository;

    // Länderspezifische IBAN-Längen (Auswahl DACH + gängige EU)
    private static readonly Dictionary<string, int> IbanLengthByCountry = new()
    {
        { "CH", 21 }, { "DE", 22 }, { "AT", 20 }, { "LI", 21 },
        { "FR", 27 }, { "IT", 27 }, { "NL", 18 }, { "BE", 16 },
        { "GB", 22 }, { "LU", 20 }
    };

    public PaymentService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    /// <inheritdoc/>
    public PaymentResult ReceiveIncomingPayment(IncomingPayment payment)
    {
        // Schritt 1: IBAN des Empfängers validieren
        if (!ValidateIban(payment.CreditorIban))
            return PaymentResult.Fail("INVALID_CREDITOR_IBAN");

        // Schritt 2: Empfängerkonto im System suchen
        var creditorAccount = _accountRepository.FindByIban(payment.CreditorIban);
        if (creditorAccount is null)
            return PaymentResult.Fail("CREDITOR_ACCOUNT_NOT_FOUND");

        // Schritt 3: Konto muss aktiv sein (nicht gesperrt oder geschlossen)
        if (!creditorAccount.IsActive)
            return PaymentResult.Fail("CREDITOR_ACCOUNT_NOT_ACTIVE");

        // Schritt 4: Betrag muss positiv sein
        if (payment.Amount.Amount <= 0)
            return PaymentResult.Fail("INVALID_AMOUNT");

        // Schritt 5: Gutschrift buchen (Delegation an Account-Context)
        var creditResult = new AccountService(_accountRepository)
            .CreditAccount(payment.CreditorIban, payment.Amount, payment.RemittanceInfo);

        if (!creditResult.IsSuccess)
            return PaymentResult.Fail(creditResult.ErrorCode ?? "CREDIT_FAILED");

        return PaymentResult.Ok(Guid.NewGuid());
    }

    /// <inheritdoc/>
    public bool ValidateIban(string iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
            return false;

        // Normalisieren: Leerzeichen entfernen, Grossbuchstaben
        var normalized = iban.Replace(" ", "").ToUpperInvariant();

        // Basisformat: 2 Buchstaben (Ländercode) + 2 Ziffern (Prüfziffer) + BBAN
        if (!Regex.IsMatch(normalized, @"^[A-Z]{2}\d{2}[A-Z0-9]+$"))
            return false;

        // Ländercode extrahieren und Länge prüfen
        var countryCode = normalized[..2];
        if (IbanLengthByCountry.TryGetValue(countryCode, out int expectedLength))
        {
            if (normalized.Length != expectedLength)
                return false;
        }
        else
        {
            // Unbekanntes Land: Mindestlänge 15, Maximallänge 34 (ISO 13616)
            if (normalized.Length < 15 || normalized.Length > 34)
                return false;
        }

        // Mod-97-Prüfzifferberechnung (ISO 13616)
        // Schritt: Erste 4 Zeichen ans Ende verschieben, Buchstaben in Ziffern konvertieren
        var rearranged = normalized[4..] + normalized[..4];
        var numericString = string.Concat(rearranged.Select(c =>
            char.IsLetter(c) ? (c - 'A' + 10).ToString() : c.ToString()));

        // BigInteger für Mod-97 (Zahl zu gross für long)
        return BigInteger.Parse(numericString) % 97 == 1;
    }
}
