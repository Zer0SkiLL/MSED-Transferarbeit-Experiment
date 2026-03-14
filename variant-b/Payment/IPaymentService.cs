namespace BankApp.Payment;

// ============================================================
// IPaymentService – Bounded Context: Payment
// ============================================================

/// <summary>
/// Service-Interface für die Verarbeitung von Zahlungsaufträgen.
/// Zuständig für IBAN-Validierung, Zahlungsverarbeitung und
/// Gutschrift auf Empfängerkonten.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Verarbeitet einen eingehenden Zahlungsauftrag.
    /// Validiert die IBAN des Empfängers (Struktur + Prüfziffer),
    /// prüft ob das Empfängerkonto aktiv ist, und schreibt die Gutschrift.
    /// </summary>
    /// <param name="payment">Der eingehende Zahlungsauftrag (ISO 20022 CreditTransfer).</param>
    /// <returns>Ergebnis mit PaymentId bei Erfolg oder ErrorCode bei Ablehnung.</returns>
    PaymentResult ReceiveIncomingPayment(IncomingPayment payment);

    /// <summary>
    /// Initiiert eine ausgehende Überweisung.
    /// Validiert die Ziel-IBAN nach ISO 13616, prüft ob das Quellkonto aktiv ist,
    /// stellt sicher dass Betrag plus Gebühr (0.50 CHF) den Saldo nicht übersteigt,
    /// belastet das Konto und schreibt einen Audit-Log-Eintrag.
    /// </summary>
    /// <param name="transfer">Der ausgehende Zahlungsauftrag inkl. Initiator-ID.</param>
    /// <returns>Ergebnis mit PaymentId bei Erfolg oder ErrorCode bei Ablehnung.</returns>
    PaymentResult InitiateOutgoingTransfer(OutgoingTransfer transfer);

    /// <summary>
    /// Validiert eine IBAN nach ISO 13616 (Ländercode, Prüfziffer, Länge).
    /// </summary>
    /// <param name="iban">Zu prüfende IBAN (mit oder ohne Leerzeichen).</param>
    /// <returns>true wenn die IBAN strukturell und rechnerisch korrekt ist.</returns>
    bool ValidateIban(string iban);
}
