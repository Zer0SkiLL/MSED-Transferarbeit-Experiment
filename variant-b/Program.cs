// Variante B – AI-Ready Entry Point
// Dependency Injection über Microsoft.Extensions.DependencyInjection
// würde in Produktion hier konfiguriert – vereinfacht für PoC-Demo.

using BankApp.Account;
using BankApp.Payment;
using BankApp.Customer;

// Repositories (In-Memory für PoC)
IAccountRepository accountRepo = new InMemoryAccountRepository();
ICustomerRepository customerRepo = new InMemoryCustomerRepository();

// Services mit expliziten Abhängigkeiten (Interfaces)
IAccountService accountService = new AccountService(accountRepo);
IPaymentService paymentService = new PaymentService(accountRepo);

// Demo-Ausführung der drei Experiment-Tasks
Console.WriteLine("=== PoC Variante B – AI-Ready ===\n");

// T1: IBAN-Validierung beim Zahlungseingang
var paymentResult = paymentService.ReceiveIncomingPayment(new IncomingPayment(
    DebtorIban: "DE89370400440532013000",
    CreditorIban: "CH5604835012345678009",
    Amount: new Money(500.00m, Currency.CHF),
    RemittanceInfo: "Gutschrift Auftrag 4711"
));
Console.WriteLine($"T1 Zahlungseingang: {(paymentResult.IsSuccess ? "OK" : "FEHLER – " + paymentResult.ErrorCode)}");

// T2: Konten mit negativem Saldo
var negativeAccounts = accountService.GetAccountsWithNegativeBalance();
Console.WriteLine($"\nT2 Konten mit negativem Saldo ({negativeAccounts.Count}):");
foreach (var acc in negativeAccounts)
    Console.WriteLine($"  IBAN={acc.Iban}  Saldo={acc.Balance.Amount:F2} {acc.Balance.Currency}  Status={acc.Status}");

// T3: Konto sperren mit Audit-Log
var blockResult = accountService.BlockAccount(
    iban: "CH5604835012345678012",
    reason: BlockReason.SuspectedFraud,
    initiatedBy: "compliance-officer@bank.ch"
);
Console.WriteLine($"\nT3 Konto gesperrt: {(blockResult.IsSuccess ? "OK" : "FEHLER – " + blockResult.ErrorCode)}");
