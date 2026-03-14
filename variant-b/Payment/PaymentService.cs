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
    public PaymentResult InitiateOutgoingTransfer(OutgoingTransfer transfer)
    {
        // 1. Validate IBANs
        if (!ValidateIban(transfer.DebtorIban))
            return PaymentResult.Fail("INVALID_DEBTOR_IBAN");
        
        if (!ValidateIban(transfer.CreditorIban))
            return PaymentResult.Fail("INVALID_CREDITOR_IBAN");

        // 2. Fetch Debtor Account
        var debtorAccount = _accountRepository.FindByIban(transfer.DebtorIban);
        if (debtorAccount is null)
            return PaymentResult.Fail("DEBTOR_ACCOUNT_NOT_FOUND");

        // 3. Check if account is active
        if (!debtorAccount.IsActive)
            return PaymentResult.Fail("DEBTOR_ACCOUNT_NOT_ACTIVE");

        // 4. Verify balance (Amount + 0.50 CHF fee)
        // Note: Assumes the transfer is in CHF as per fee requirement
        var fee = new Money(0.50m, Currency.CHF);
        
        // Ensure currencies match for calculation
        if (transfer.Amount.Currency != Currency.CHF)
            return PaymentResult.Fail("UNSUPPORTED_CURRENCY_FOR_FEE_CALCULATION");

        var totalAmount = transfer.Amount.Add(fee);
        if (debtorAccount.Balance.Amount < totalAmount.Amount)
            return PaymentResult.Fail("INSUFFICIENT_FUNDS");

        // 5. Deduct amount and fee
        var updatedAccount = debtorAccount with 
        { 
            Balance = debtorAccount.Balance.Subtract(totalAmount) 
        };
        _accountRepository.Save(updatedAccount);

        // 6. Append Audit Entry
        _accountRepository.AppendAuditEntry(new AccountAuditEntry(
            EntryId: Guid.NewGuid(),
            Iban: transfer.DebtorIban,
            EventType: "OUTGOING_TRANSFER",
            Description: $"Transfer of {transfer.Amount.Amount} {transfer.Amount.Currency} to {transfer.CreditorIban}. Fee: {fee.Amount} {fee.Currency}.",
            InitiatedBy: transfer.InitiatedBy,
            OccurredAt: DateTimeOffset.UtcNow
        ));

        return PaymentResult.Ok(Guid.NewGuid());
    }

    /// <inheritdoc/>
    public bool ValidateIban(string iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
            return false;

        // 1. Normalize: remove spaces and convert to uppercase
        var normalized = iban.Replace(" ", "").ToUpperInvariant();

        // 2. Preliminary format check: 2 letters (country), 2 digits (checksum), followed by alphanumeric BBAN
        // ISO 13616 specifies a maximum length of 34 characters.
        if (normalized.Length < 5 || normalized.Length > 34 || !Regex.IsMatch(normalized, @"^[A-Z]{2}\d{2}[A-Z0-9]+$"))
            return false;

        // 3. Optional: Specific country length validation (if known)
        var countryCode = normalized[..2];
        if (IbanLengthByCountry.TryGetValue(countryCode, out int expectedLength))
        {
            if (normalized.Length != expectedLength)
                return false;
        }

        // 4. Rearrange: Move first 4 characters to the end
        // [CC][PP][BBAN...] -> [BBAN...][CC][PP]
        var rearranged = normalized[4..] + normalized[..4];

        // 5. Convert characters to digits: A=10, B=11, ..., Z=35
        var numericString = string.Concat(rearranged.Select(c =>
            char.IsLetter(c) ? (c - 'A' + 10).ToString() : c.ToString()));

        // 6. Perform Modulo-97 operation
        // The result must be 1 for the IBAN to be valid according to ISO 13616.
        if (BigInteger.TryParse(numericString, out BigInteger ibanNumber))
        {
            return ibanNumber % 97 == 1;
        }

        return false;
    }
}
