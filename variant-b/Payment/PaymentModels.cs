namespace BankApp.Payment;

using BankApp.Account;

// ============================================================
// Domain Models – Bounded Context: Payment
// ============================================================

/// <summary>
/// Eingehender Zahlungsauftrag (SEPA Credit Transfer / SIC).
/// Alle Felder entsprechen der ISO 20022-Terminologie.
/// </summary>
/// <param name="DebtorIban">IBAN des Zahlungspflichtigen (Absender).</param>
/// <param name="CreditorIban">IBAN des Zahlungsempfängers (Empfänger, muss im System vorhanden sein).</param>
/// <param name="Amount">Betrag mit Währung.</param>
/// <param name="RemittanceInfo">Verwendungszweck (max. 140 Zeichen, ISO 20022).</param>
public record IncomingPayment(
    string DebtorIban,
    string CreditorIban,
    Money Amount,
    string RemittanceInfo
);

/// <summary>
/// Ergebnis der Verarbeitung eines Zahlungsauftrags.
/// </summary>
public record PaymentResult(bool IsSuccess, string? ErrorCode = null, Guid? PaymentId = null)
{
    public static PaymentResult Ok(Guid paymentId) => new(true, null, paymentId);
    public static PaymentResult Fail(string errorCode) => new(false, errorCode);
}
