// ==============================================================
// BankSystem.cs  –  Variante A: Legacy-Monolith (God-Class)
//
// Absichtlich schlecht strukturiert für PoC-Vergleich:
//   - Keine Interfaces
//   - Keine Bounded Contexts / Separation of Concerns
//   - Technisch/generische Namensgebung (kein Ubiquitous Language)
//   - Keine XML-Dokumentation
//   - Keine OpenAPI-Spezifikation
//   - Alle Domänen (Konto, Zahlung, Kunde, Audit) in einer Klasse
//   - Datenhaltung in In-Memory-Listen ohne Abstraktionsschicht
// ==============================================================

using System.Text.RegularExpressions;

public class BankSystem
{
    // ----------------------------------------------------------------
    // Interne Datenhaltung – alles in primitiven Listen
    // ----------------------------------------------------------------
    private List<object[]> _dataRows = new();        // Konten
    private List<object[]> _txRows = new();          // Transaktionen
    private List<object[]> _custRows = new();        // Kunden
    private List<string[]> _logRows = new();         // Audit-Log
    private int _nextId = 1000;

    // ----------------------------------------------------------------
    // Einstiegspunkt
    // ----------------------------------------------------------------
    public void Run()
    {
        Init();
        Console.WriteLine("BankSystem bereit.");
    }

    // ----------------------------------------------------------------
    // Initialisierung mit Testdaten
    // ----------------------------------------------------------------
    private void Init()
    {
        // Kunden anlegen
        _custRows.Add(new object[] { 1, "Hans Muster", "hans.muster@example.com", true });
        _custRows.Add(new object[] { 2, "Maria Müller", "maria.mueller@example.com", true });
        _custRows.Add(new object[] { 3, "Peter Schmid", "peter.schmid@example.com", false });

        // Konten anlegen: [id, customerId, type, balance, currency, status, iban]
        _dataRows.Add(new object[] { 1001, 1, "CHK", 1500.00m, "CHF", "ACTIVE", "CH5604835012345678009" });
        _dataRows.Add(new object[] { 1002, 1, "SAV", -200.50m, "CHF", "ACTIVE", "CH5604835012345678010" });
        _dataRows.Add(new object[] { 1003, 2, "CHK",  800.00m, "CHF", "ACTIVE", "CH5604835012345678011" });
        _dataRows.Add(new object[] { 1004, 2, "SAV", -1050.75m,"CHF", "BLOCKED","CH5604835012345678012" });
        _dataRows.Add(new object[] { 1005, 3, "CHK",    0.00m, "CHF", "ACTIVE", "CH5604835012345678013" });
    }

    // ----------------------------------------------------------------
    // T1 – Zahlung verarbeiten (IBAN-Validierung fehlt / rudimentär)
    // ----------------------------------------------------------------
    public bool ProcessPayment(int fromAccId, string toIban, decimal amount, string ref1)
    {
        // Konto suchen – keine Abstraktion, direkter Array-Zugriff
        object[]? src = null;
        foreach (var r in _dataRows)
            if ((int)r[0] == fromAccId) { src = r; break; }

        if (src == null) return false;
        if ((string)src[5] != "ACTIVE") return false;
        if ((decimal)src[3] < amount) return false;

        // Rudimentäre IBAN-Prüfung: nur Länge
        if (toIban == null || toIban.Length < 15) return false;

        // Buchung
        src[3] = (decimal)src[3] - amount;
        _txRows.Add(new object[] { ++_nextId, fromAccId, toIban, amount, DateTime.Now, ref1, "OUT" });
        _logRows.Add(new string[] { DateTime.Now.ToString("o"), "TX", $"Payment {amount} from {fromAccId} to {toIban}" });
        return true;
    }

    // ----------------------------------------------------------------
    // T2 – Alle Konten mit negativem Saldo ausgeben
    // ----------------------------------------------------------------
    public void PrintNegativeBalances()
    {
        Console.WriteLine("Negative Salden:");
        foreach (var r in _dataRows)
        {
            decimal bal = (decimal)r[3];
            if (bal < 0)
                Console.WriteLine($"  AccId={r[0]}  Bal={bal:F2}  Ccy={r[4]}  Status={r[5]}");
        }
    }

    // T2 – als Liste für programmatischen Zugriff
    public List<object[]> GetNegativeBalanceAccounts()
    {
        var result = new List<object[]>();
        foreach (var r in _dataRows)
            if ((decimal)r[3] < 0) result.Add(r);
        return result;
    }

    // ----------------------------------------------------------------
    // T3 – Konto sperren
    // ----------------------------------------------------------------
    public bool BlockAccount(int accId, string reason)
    {
        foreach (var r in _dataRows)
        {
            if ((int)r[0] == accId)
            {
                r[5] = "BLOCKED";
                _logRows.Add(new string[] {
                    DateTime.Now.ToString("o"),
                    "BLOCK",
                    $"Account {accId} blocked. Reason: {reason}"
                });
                return true;
            }
        }
        return false;
    }

    // ----------------------------------------------------------------
    // Diverses – alles in derselben Klasse
    // ----------------------------------------------------------------

    public decimal GetBalance(int accId)
    {
        foreach (var r in _dataRows)
            if ((int)r[0] == accId) return (decimal)r[3];
        return -9999m; // magic number als Fehlerindikator
    }

    public bool Deposit(int accId, decimal amount)
    {
        foreach (var r in _dataRows)
        {
            if ((int)r[0] == accId)
            {
                r[3] = (decimal)r[3] + amount;
                _txRows.Add(new object[] { ++_nextId, accId, null!, amount, DateTime.Now, "DEP", "IN" });
                return true;
            }
        }
        return false;
    }

    public void PrintAuditLog()
    {
        foreach (var e in _logRows)
            Console.WriteLine($"[{e[0]}] {e[1]}: {e[2]}");
    }

    // Hilfsmethode – kein klarer Kontext, woher sie gehört
    private bool CheckIban(string s)
    {
        return s != null && s.Length >= 15 && s.Length <= 34;
    }

    public void AddCustomer(string name, string email)
    {
        int newId = _custRows.Count + 1;
        _custRows.Add(new object[] { newId, name, email, true });
    }

    public void PrintCustomers()
    {
        foreach (var c in _custRows)
            Console.WriteLine($"Cust {c[0]}: {c[1]} ({c[2]}) Active={c[3]}");
    }
}
