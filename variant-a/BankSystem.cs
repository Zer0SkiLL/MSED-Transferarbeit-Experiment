// ==============================================================
// BankSystem.cs  –  Variante A: Legacy-Monolith (God-Class)
//
// Absichtlich schlecht strukturiert für PoC-Vergleich.
// Diese Klasse enthält sämtliche Domänen-Logik der Kernbank:
//   Kontoführung, Zahlungsverkehr, Kundenverwaltung, Compliance,
//   Berichtswesen, Zins/Gebühren, Daueraufträge, Limits, Audit.
//
// Anti-Patterns (bewusst eingesetzt):
//   - Keine Interfaces, kein Dependency Injection
//   - Keine Bounded Contexts / Separation of Concerns
//   - Technisch/generische Namensgebung (kein Ubiquitous Language)
//   - Keine XML-Dokumentation
//   - Magic Numbers und Magic Strings überall
//   - Copy-Paste statt Wiederverwendung
//   - Datenhaltung in primitiven object[]-Listen
//   - Tote Kommentare und auskommentierter Code
//   - Methoden mit zu vielen Verantwortlichkeiten
// ==============================================================

using System.Numerics;
using System.Text.RegularExpressions;

public class BankSystem
{
    // ----------------------------------------------------------------
    // Interne Datenhaltung – alles in primitiven Listen
    // Konten:       [id, customerId, type, balance, currency, status, iban, limit, openDate, interestRate]
    // Transaktionen:[id, fromAccId, toIban, amount, date, ref, direction, status, fee]
    // Kunden:       [id, name, email, active, riskLevel, kycDate, address, phone]
    // Audit-Log:    [timestamp, category, message, userId, severity]
    // Daueraufträge:[id, fromAccId, toIban, amount, dayOfMonth, lastRun, active, ref]
    // Limits:       [accId, dailyLimit, monthlyLimit, usedToday, usedMonth, resetDate]
    // ----------------------------------------------------------------
    private List<object[]> _dataRows     = new();
    private List<object[]> _txRows       = new();
    private List<object[]> _custRows     = new();
    private List<object[]> _logRows      = new();
    private List<object[]> _standingOrders = new();
    private List<object[]> _limitRows    = new();
    private List<object[]> _feeRows      = new();
    private int _nextId = 1000;
    private int _nextTxId = 5000;
    private decimal _overdraftRate = 0.12m;   // 12% p.a.
    private decimal _savingsRate   = 0.005m;  // 0.5% p.a.
    // private decimal _fxMarkup   = 0.015m;  // TODO: FX-Markup reaktivieren

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
        _custRows.Add(new object[] { 1, "Hans Muster",    "hans.muster@example.com",    true,  "LOW",    new DateTime(2015,3,1),  "Bahnhofstr. 1, Zürich",     "+41791234567" });
        _custRows.Add(new object[] { 2, "Maria Müller",   "maria.mueller@example.com",  true,  "LOW",    new DateTime(2016,6,15), "Seestrasse 22, Luzern",      "+41791234568" });
        _custRows.Add(new object[] { 3, "Peter Schmid",   "peter.schmid@example.com",   false, "HIGH",   new DateTime(2018,1,10), "Rathausplatz 5, Bern",       "+41791234569" });
        _custRows.Add(new object[] { 4, "Anna Keller",    "anna.keller@example.com",    true,  "MEDIUM", new DateTime(2019,7,20), "Hauptstrasse 8, Basel",      "+41791234570" });
        _custRows.Add(new object[] { 5, "Bruno Fischer",  "bruno.fischer@example.com",  true,  "LOW",    new DateTime(2020,2,5),  "Dorfstrasse 3, Zug",         "+41791234571" });
        _custRows.Add(new object[] { 6, "Sandra Wolf",    "sandra.wolf@example.com",    true,  "LOW",    new DateTime(2021,9,12), "Gartenweg 9, Winterthur",    "+41791234572" });

        // Konten anlegen: [id, customerId, type, balance, currency, status, iban, limit, openDate, interestRate]
        _dataRows.Add(new object[] { 1001, 1, "CHK",     1500.00m,  "CHF", "ACTIVE",  "CH5604835012345678009", 5000m,  new DateTime(2015,3,1),  0.0m   });
        _dataRows.Add(new object[] { 1002, 1, "SAV",     -200.50m,  "CHF", "ACTIVE",  "CH5604835012345678010", 500m,   new DateTime(2015,3,1),  0.005m });
        _dataRows.Add(new object[] { 1003, 2, "CHK",      800.00m,  "CHF", "ACTIVE",  "CH5604835012345678011", 3000m,  new DateTime(2016,6,15), 0.0m   });
        _dataRows.Add(new object[] { 1004, 2, "SAV",    -1050.75m,  "CHF", "BLOCKED", "CH7204835012345678012", 1000m,  new DateTime(2016,6,15), 0.005m });
        _dataRows.Add(new object[] { 1005, 3, "CHK",        0.00m,  "CHF", "ACTIVE",  "CH5604835012345678013", 0m,     new DateTime(2018,1,10), 0.0m   });
        _dataRows.Add(new object[] { 1006, 4, "CHK",    12300.00m,  "CHF", "ACTIVE",  "CH5604835012345678014", 10000m, new DateTime(2019,7,20), 0.0m   });
        _dataRows.Add(new object[] { 1007, 4, "SAV",     3400.00m,  "CHF", "ACTIVE",  "CH5604835012345678015", 0m,     new DateTime(2019,7,20), 0.005m });
        _dataRows.Add(new object[] { 1008, 5, "CHK",      -88.30m,  "CHF", "ACTIVE",  "CH5604835012345678016", 2000m,  new DateTime(2020,2,5),  0.0m   });
        _dataRows.Add(new object[] { 1009, 6, "CHK",     2200.00m,  "EUR", "ACTIVE",  "CH5604835012345678017", 5000m,  new DateTime(2021,9,12), 0.0m   });
        _dataRows.Add(new object[] { 1010, 6, "SAV",      900.00m,  "EUR", "FROZEN",  "CH5604835012345678018", 0m,     new DateTime(2021,9,12), 0.005m });

        // Limits initialisieren
        foreach (var acc in _dataRows)
            _limitRows.Add(new object[] { (int)acc[0], (decimal)acc[7], (decimal)acc[7] * 10, 0m, 0m, DateTime.Today });

        // Testdaten Transaktionen
        _txRows.Add(new object[] { 5001, 1001, "CH5604835012345678011", 200.00m, new DateTime(2026,1,5),  "Miete Jan", "OUT", "SETTLED", 0.50m });
        _txRows.Add(new object[] { 5002, 1003, "CH5604835012345678009", 200.00m, new DateTime(2026,1,5),  "Miete Jan", "IN",  "SETTLED", 0.00m });
        _txRows.Add(new object[] { 5003, 1001, "DE89370400440532013000", 350.00m, new DateTime(2026,2,1), "Rechnung",  "OUT", "SETTLED", 1.50m });
        _nextTxId = 5010;
        _nextId   = 1020;
    }

    // ----------------------------------------------------------------
    // T1 – Zahlung verarbeiten (IBAN-Validierung rudimentär)
    // ----------------------------------------------------------------
    public bool ProcessPayment(int fromAccId, string toIban, decimal amount, string ref1)
    {
        object[]? src = null;
        foreach (var r in _dataRows)
            if ((int)r[0] == fromAccId) { src = r; break; }

        if (src == null) return false;
        if ((string)src[5] != "ACTIVE") return false;
        if ((decimal)src[3] < amount) return false;

        // Rudimentäre IBAN-Prüfung: nur Länge
        if (toIban == null || toIban.Length < 15) return false;

        // Tages-Limit prüfen
        object[]? lim = null;
        foreach (var l in _limitRows)
            if ((int)l[0] == fromAccId) { lim = l; break; }
        if (lim != null)
        {
            decimal used = (decimal)lim[3];
            decimal daily = (decimal)lim[1];
            if (used + amount > daily)
            {
                _logRows.Add(new object[] { DateTime.Now.ToString("o"), "LIMIT", $"Tageslimit überschritten für Konto {fromAccId}", "system", "WARN" });
                return false;
            }
            lim[3] = used + amount;
        }

        decimal fee = amount > 1000m ? 1.50m : 0.50m;
        src[3] = (decimal)src[3] - amount - fee;
        _txRows.Add(new object[] { ++_nextTxId, fromAccId, toIban, amount, DateTime.Now, ref1, "OUT", "SETTLED", fee });
        _feeRows.Add(new object[] { _nextTxId, fromAccId, fee, DateTime.Now, "TX_FEE" });
        _logRows.Add(new object[] { DateTime.Now.ToString("o"), "TX", $"Payment {amount} from {fromAccId} to {toIban}", "system", "INFO" });
        return true;
    }

    // Eingehende Zahlung buchen – nahezu identisch mit ProcessPayment, aber kein Limit-Check
    public bool ReceivePayment(string fromIban, int toAccId, decimal amount, string ref1)
    {
        object[]? dst = null;
        foreach (var r in _dataRows)
            if ((int)r[0] == toAccId) { dst = r; break; }

        if (dst == null) return false;
        if ((string)dst[5] == "BLOCKED" || (string)dst[5] == "CLOSED") return false;
        if (fromIban == null || fromIban.Length < 15) return false;  // gleiche rudimentäre Prüfung wie oben

        dst[3] = (decimal)dst[3] + amount;
        _txRows.Add(new object[] { ++_nextTxId, 0, fromIban, amount, DateTime.Now, ref1, "IN", "SETTLED", 0m });
        _logRows.Add(new object[] { DateTime.Now.ToString("o"), "TX", $"Incoming {amount} to {toAccId} from {fromIban}", "system", "INFO" });
        return true;
    }

    // Massenüberweisung – Copy-Paste von ProcessPayment mit kleinen Variationen
    public int ProcessBulkPayments(int fromAccId, List<(string toIban, decimal amount, string refText)> items)
    {
        int ok = 0;
        foreach (var item in items)
        {
            object[]? src = null;
            foreach (var r in _dataRows)
                if ((int)r[0] == fromAccId) { src = r; break; }

            if (src == null) continue;
            if ((string)src[5] != "ACTIVE") continue;
            if ((decimal)src[3] < item.amount) continue;
            if (item.toIban == null || item.toIban.Length < 15) continue;

            decimal fee = 0.30m; // Bulk-Rabatt
            src[3] = (decimal)src[3] - item.amount - fee;
            _txRows.Add(new object[] { ++_nextTxId, fromAccId, item.toIban, item.amount, DateTime.Now, item.refText, "OUT", "SETTLED", fee });
            ok++;
        }
        _logRows.Add(new object[] { DateTime.Now.ToString("o"), "BULK", $"Bulk: {ok}/{items.Count} OK von Konto {fromAccId}", "system", "INFO" });
        return ok;
    }

    public bool CancelTransaction(int txId)
    {
        foreach (var tx in _txRows)
        {
            if ((int)tx[0] == txId && (string)tx[7] == "SETTLED")
            {
                tx[7] = "CANCELLED";
                _logRows.Add(new object[] { DateTime.Now.ToString("o"), "TX_CANCEL", $"Transaction {txId} cancelled", "user", "INFO" });
                return true;
            }
        }
        return false;
    }

    // ----------------------------------------------------------------
    // T2 – Konten mit negativem Saldo
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

    public List<object[]> GetNegativeBalanceAccounts()
    {
        var result = new List<object[]>();
        foreach (var r in _dataRows)
            if ((decimal)r[3] < 0) result.Add(r);
        return result;
    }

    // Gleiche Logik, anderer Name – historisch gewachsen
    public List<int> GetOverdrawnAccountIds()
    {
        var ids = new List<int>();
        foreach (var r in _dataRows)
            if ((decimal)r[3] < 0m && (string)r[5] != "CLOSED")
                ids.Add((int)r[0]);
        return ids;
    }

    public decimal GetTotalOverdraftExposure()
    {
        decimal total = 0m;
        foreach (var r in _dataRows)
        {
            decimal b = (decimal)r[3];
            if (b < 0m) total += Math.Abs(b);
        }
        return total;
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
                if ((string)r[5] == "BLOCKED") return false;
                r[5] = "BLOCKED";
                _logRows.Add(new object[] {
                    DateTime.Now.ToString("o"), "BLOCK",
                    $"Account {accId} blocked. Reason: {reason}", "system", "WARN"
                });
                // TODO: Benachrichtigung an Kunden – noch nicht implementiert
                return true;
            }
        }
        return false;
    }

    public bool UnblockAccount(int accId, string approvedBy)
    {
        foreach (var r in _dataRows)
        {
            if ((int)r[0] == accId && (string)r[5] == "BLOCKED")
            {
                r[5] = "ACTIVE";
                _logRows.Add(new object[] {
                    DateTime.Now.ToString("o"), "UNBLOCK",
                    $"Account {accId} unblocked by {approvedBy}", approvedBy, "INFO"
                });
                return true;
            }
        }
        return false;
    }

    public bool FreezeAccount(int accId)
    {
        foreach (var r in _dataRows)
        {
            if ((int)r[0] == accId && (string)r[5] == "ACTIVE")
            {
                r[5] = "FROZEN";
                _logRows.Add(new object[] { DateTime.Now.ToString("o"), "FREEZE", $"Account {accId} frozen", "compliance", "WARN" });
                return true;
            }
        }
        return false;
    }

    // ----------------------------------------------------------------
    // Kontoverwaltung
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
                if ((string)r[5] == "CLOSED") return false;
                r[3] = (decimal)r[3] + amount;
                _txRows.Add(new object[] { ++_nextTxId, accId, null!, amount, DateTime.Now, "DEPOSIT", "IN", "SETTLED", 0m });
                _logRows.Add(new object[] { DateTime.Now.ToString("o"), "DEPOSIT", $"Deposit {amount} to {accId}", "teller", "INFO" });
                return true;
            }
        }
        return false;
    }

    public bool Withdraw(int accId, decimal amount)
    {
        foreach (var r in _dataRows)
        {
            if ((int)r[0] == accId)
            {
                if ((string)r[5] != "ACTIVE") return false;
                decimal bal = (decimal)r[3];
                decimal limit = (decimal)r[7];
                if (bal - amount < -limit) return false;
                r[3] = bal - amount;
                decimal fee = amount >= 200m ? 0.00m : 0.00m; // Bargeld-Gebühr zukünftig
                _txRows.Add(new object[] { ++_nextTxId, accId, null!, amount, DateTime.Now, "WITHDRAWAL", "OUT", "SETTLED", fee });
                return true;
            }
        }
        return false;
    }

    public int CreateAccount(int customerId, string type, string currency, decimal initialDeposit)
    {
        // Prüfen ob Kunde existiert
        bool custExists = false;
        foreach (var c in _custRows)
            if ((int)c[0] == customerId && (bool)c[3]) { custExists = true; break; }
        if (!custExists) return -1;

        int newId = ++_nextId;
        string iban = $"CH00000000000000{newId}"; // Platzhalter-IBAN
        decimal rate = type == "SAV" ? _savingsRate : 0.0m;
        _dataRows.Add(new object[] { newId, customerId, type, initialDeposit, currency, "ACTIVE", iban, 1000m, DateTime.Today, rate });
        _limitRows.Add(new object[] { newId, 1000m, 10000m, 0m, 0m, DateTime.Today });
        _logRows.Add(new object[] { DateTime.Now.ToString("o"), "ACCT_OPEN", $"Account {newId} opened for customer {customerId}", "system", "INFO" });
        return newId;
    }

    public bool CloseAccount(int accId)
    {
        foreach (var r in _dataRows)
        {
            if ((int)r[0] == accId && (string)r[5] != "CLOSED")
            {
                if ((decimal)r[3] != 0m) return false; // Konto muss ausgeglichen sein
                r[5] = "CLOSED";
                _logRows.Add(new object[] { DateTime.Now.ToString("o"), "ACCT_CLOSE", $"Account {accId} closed", "system", "INFO" });
                return true;
            }
        }
        return false;
    }

    public string GetAccountStatus(int accId)
    {
        foreach (var r in _dataRows)
            if ((int)r[0] == accId) return (string)r[5];
        return "NOT_FOUND";
    }

    public List<int> GetAccountsByCustomer(int customerId)
    {
        var ids = new List<int>();
        foreach (var r in _dataRows)
            if ((int)r[1] == customerId) ids.Add((int)r[0]);
        return ids;
    }

    // Interner Transfer zwischen zwei eigenen Konten
    public bool InternalTransfer(int fromAccId, int toAccId, decimal amount, string ref1)
    {
        object[]? src = null, dst = null;
        foreach (var r in _dataRows)
        {
            if ((int)r[0] == fromAccId) src = r;
            if ((int)r[0] == toAccId)   dst = r;
        }
        if (src == null || dst == null) return false;
        if ((string)src[5] != "ACTIVE") return false;
        if ((string)dst[5] == "CLOSED" || (string)dst[5] == "BLOCKED") return false;
        if ((decimal)src[3] < amount) return false;

        src[3] = (decimal)src[3] - amount;
        dst[3] = (decimal)dst[3] + amount;
        _txRows.Add(new object[] { ++_nextTxId, fromAccId, dst[6], amount, DateTime.Now, ref1, "INT", "SETTLED", 0m });
        _logRows.Add(new object[] { DateTime.Now.ToString("o"), "INT_TX", $"Internal transfer {amount} from {fromAccId} to {toAccId}", "system", "INFO" });
        return true;
    }

    // ----------------------------------------------------------------
    // Kundenverwaltung
    // ----------------------------------------------------------------
    public void AddCustomer(string name, string email)
    {
        int newId = _custRows.Count + 1;
        _custRows.Add(new object[] { newId, name, email, true, "LOW", DateTime.Today, "", "" });
    }

    public bool UpdateCustomerEmail(int custId, string newEmail)
    {
        foreach (var c in _custRows)
        {
            if ((int)c[0] == custId)
            {
                c[2] = newEmail;
                _logRows.Add(new object[] { DateTime.Now.ToString("o"), "CUST_UPD", $"Email updated for customer {custId}", "user", "INFO" });
                return true;
            }
        }
        return false;
    }

    public bool DeactivateCustomer(int custId)
    {
        foreach (var c in _custRows)
        {
            if ((int)c[0] == custId && (bool)c[3])
            {
                c[3] = false;
                _logRows.Add(new object[] { DateTime.Now.ToString("o"), "CUST_DEACT", $"Customer {custId} deactivated", "admin", "WARN" });
                return true;
            }
        }
        return false;
    }

    public object[]? GetCustomer(int custId)
    {
        foreach (var c in _custRows)
            if ((int)c[0] == custId) return c;
        return null;
    }

    public List<object[]> GetAllCustomers() => new List<object[]>(_custRows);

    public void PrintCustomers()
    {
        foreach (var c in _custRows)
            Console.WriteLine($"Cust {c[0]}: {c[1]} ({c[2]}) Active={c[3]} Risk={c[4]}");
    }

    // ----------------------------------------------------------------
    // Limit-Verwaltung
    // ----------------------------------------------------------------
    public bool UpdateDailyLimit(int accId, decimal newLimit)
    {
        foreach (var l in _limitRows)
        {
            if ((int)l[0] == accId)
            {
                l[1] = newLimit;
                _logRows.Add(new object[] { DateTime.Now.ToString("o"), "LIMIT_UPD", $"Daily limit {newLimit} set for {accId}", "admin", "INFO" });
                return true;
            }
        }
        return false;
    }

    public decimal GetRemainingDailyLimit(int accId)
    {
        foreach (var l in _limitRows)
            if ((int)l[0] == accId) return (decimal)l[1] - (decimal)l[3];
        return 0m;
    }

    private void ResetDailyLimits()
    {
        foreach (var l in _limitRows)
        {
            if ((DateTime)l[5] < DateTime.Today)
            {
                l[3] = 0m;
                l[5] = DateTime.Today;
            }
        }
    }

    // ----------------------------------------------------------------
    // Daueraufträge
    // ----------------------------------------------------------------
    public int AddStandingOrder(int fromAccId, string toIban, decimal amount, int dayOfMonth, string ref1)
    {
        if (dayOfMonth < 1 || dayOfMonth > 28) return -1;
        if (toIban == null || toIban.Length < 15) return -1;

        int newId = ++_nextId;
        _standingOrders.Add(new object[] { newId, fromAccId, toIban, amount, dayOfMonth, DateTime.MinValue, true, ref1 });
        _logRows.Add(new object[] { DateTime.Now.ToString("o"), "SO_ADD", $"Standing order {newId} added for {fromAccId}", "user", "INFO" });
        return newId;
    }

    public bool CancelStandingOrder(int soId)
    {
        foreach (var so in _standingOrders)
        {
            if ((int)so[0] == soId && (bool)so[6])
            {
                so[6] = false;
                _logRows.Add(new object[] { DateTime.Now.ToString("o"), "SO_CANCEL", $"Standing order {soId} cancelled", "user", "INFO" });
                return true;
            }
        }
        return false;
    }

    public int ProcessStandingOrders()
    {
        int processed = 0;
        int today = DateTime.Today.Day;
        foreach (var so in _standingOrders)
        {
            if (!(bool)so[6]) continue;
            if ((int)so[4] != today) continue;
            if ((DateTime)so[5] >= DateTime.Today) continue; // heute schon ausgeführt

            bool ok = ProcessPayment((int)so[1], (string)so[2], (decimal)so[3], (string)so[7]);
            if (ok)
            {
                so[5] = DateTime.Today;
                processed++;
                _logRows.Add(new object[] { DateTime.Now.ToString("o"), "SO_EXEC", $"Standing order {so[0]} executed", "system", "INFO" });
            }
            else
            {
                _logRows.Add(new object[] { DateTime.Now.ToString("o"), "SO_FAIL", $"Standing order {so[0]} failed – insufficient funds or limit", "system", "ERROR" });
            }
        }
        return processed;
    }

    // ----------------------------------------------------------------
    // Zins und Gebühren
    // ----------------------------------------------------------------
    public void ApplyMonthlyFees()
    {
        foreach (var r in _dataRows)
        {
            if ((string)r[5] == "CLOSED") continue;
            string type = (string)r[2];
            decimal fee = type == "CHK" ? 3.50m : 0.00m; // Kontoführungsgebühr
            if (fee > 0)
            {
                r[3] = (decimal)r[3] - fee;
                _feeRows.Add(new object[] { ++_nextTxId, (int)r[0], fee, DateTime.Now, "MONTHLY_FEE" });
                _logRows.Add(new object[] { DateTime.Now.ToString("o"), "FEE", $"Monthly fee {fee} charged to {r[0]}", "system", "INFO" });
            }
        }
    }

    public void CalculateAndApplyInterest()
    {
        foreach (var r in _dataRows)
        {
            if ((string)r[5] == "CLOSED") continue;
            decimal bal = (decimal)r[3];
            decimal rate = (decimal)r[9];

            if (bal > 0 && rate > 0)
            {
                decimal interest = Math.Round(bal * rate / 12m, 2); // monatlich
                r[3] = bal + interest;
                _logRows.Add(new object[] { DateTime.Now.ToString("o"), "INTEREST", $"Interest {interest} credited to {r[0]}", "system", "INFO" });
            }
            else if (bal < 0)
            {
                decimal overdraft = Math.Round(Math.Abs(bal) * _overdraftRate / 12m, 2);
                r[3] = bal - overdraft;
                _logRows.Add(new object[] { DateTime.Now.ToString("o"), "OVERDRAFT_FEE", $"Overdraft fee {overdraft} charged to {r[0]}", "system", "WARN" });
            }
        }
    }

    public decimal GetTotalFeeRevenue()
    {
        decimal total = 0m;
        foreach (var f in _feeRows)
            total += (decimal)f[2];
        return total;
    }

    // ----------------------------------------------------------------
    // Transaktionshistorie
    // ----------------------------------------------------------------
    public List<object[]> GetTransactionHistory(int accId)
    {
        var result = new List<object[]>();
        foreach (var tx in _txRows)
            if ((int)tx[1] == accId) result.Add(tx);
        return result;
    }

    public List<object[]> GetPendingTransactions()
    {
        var result = new List<object[]>();
        foreach (var tx in _txRows)
            if ((string)tx[7] == "PENDING") result.Add(tx);
        return result;
    }

    public decimal GetDailyVolume(DateTime date)
    {
        decimal vol = 0m;
        foreach (var tx in _txRows)
        {
            DateTime txDate = (DateTime)tx[4];
            if (txDate.Date == date.Date && (string)tx[6] == "OUT")
                vol += (decimal)tx[3];
        }
        return vol;
    }

    // ----------------------------------------------------------------
    // Compliance / AML
    // ----------------------------------------------------------------
    public bool CheckAmlThreshold(int accId, decimal amount)
    {
        // Beträge über 10'000 CHF müssen gemeldet werden
        if (amount >= 10000m)
        {
            _logRows.Add(new object[] { DateTime.Now.ToString("o"), "AML", $"AML threshold exceeded: {amount} on account {accId}", "compliance", "CRITICAL" });
            return false; // manuell freizugeben
        }
        return true;
    }

    public List<object[]> GetHighRiskCustomers()
    {
        var result = new List<object[]>();
        foreach (var c in _custRows)
            if ((string)c[4] == "HIGH") result.Add(c);
        return result;
    }

    public bool FlagSuspiciousActivity(int accId, string desc)
    {
        foreach (var r in _dataRows)
        {
            if ((int)r[0] == accId)
            {
                _logRows.Add(new object[] { DateTime.Now.ToString("o"), "SUSPICIOUS", $"Account {accId}: {desc}", "compliance", "CRITICAL" });
                return FreezeAccount(accId); // Konto einfrieren
            }
        }
        return false;
    }

    // Sanktionslisten-Check (simuliert)
    public bool CheckSanctionsList(string name)
    {
        // Hardcodierte Sperrliste – in Realität externe API
        string[] sanctioned = { "Maxim Volkov", "Li Wei Trading", "Offshore Holdings SA" };
        foreach (var s in sanctioned)
            if (s.Equals(name, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    // ----------------------------------------------------------------
    // Berichtswesen
    // ----------------------------------------------------------------
    public void PrintAuditLog()
    {
        foreach (var e in _logRows)
            Console.WriteLine($"[{e[0]}] {e[1]}: {e[2]}");
    }

    public List<object[]> GetAuditLog() => new List<object[]>(_logRows);

    public List<object[]> GetAuditLogByCategory(string category)
    {
        var result = new List<object[]>();
        foreach (var e in _logRows)
            if ((string)e[1] == category) result.Add(e);
        return result;
    }

    public void GenerateMonthlyStatement(int accId)
    {
        Console.WriteLine($"\n=== Kontoauszug {accId} ({DateTime.Now:yyyy-MM}) ===");
        decimal bal = GetBalance(accId);
        Console.WriteLine($"Aktueller Saldo: {bal:F2}");
        Console.WriteLine("Transaktionen:");
        DateTime monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        foreach (var tx in _txRows)
        {
            if ((int)tx[1] == accId && (DateTime)tx[4] >= monthStart)
                Console.WriteLine($"  {tx[4]:dd.MM} {tx[6]} {tx[3]:F2} Ref={tx[5]}");
        }
    }

    public void PrintAccountSummary()
    {
        Console.WriteLine("\n=== Kontenübersicht ===");
        decimal totalAssets = 0m, totalLiabilities = 0m;
        foreach (var r in _dataRows)
        {
            decimal bal = (decimal)r[3];
            if (bal >= 0) totalAssets += bal;
            else totalLiabilities += bal;
            Console.WriteLine($"  {r[0]} | Cust={r[1]} | {r[2]} | {bal:F2} {r[4]} | {r[5]}");
        }
        Console.WriteLine($"Total Aktiven: {totalAssets:F2} CHF  Passiven: {totalLiabilities:F2} CHF");
    }

    // ----------------------------------------------------------------
    // Benachrichtigungen
    // ----------------------------------------------------------------
    public void SendLowBalanceWarnings(decimal threshold)
    {
        foreach (var r in _dataRows)
        {
            decimal bal = (decimal)r[3];
            if (bal < threshold && (string)r[5] == "ACTIVE")
            {
                // Kunden-E-Mail holen – direkter Zugriff auf _custRows (keine Abstraktion)
                int custId = (int)r[1];
                string email = "";
                foreach (var c in _custRows)
                    if ((int)c[0] == custId) { email = (string)c[2]; break; }
                Console.WriteLine($"[NOTIFY] Low balance {bal:F2} on account {r[0]} – email: {email}");
                _logRows.Add(new object[] { DateTime.Now.ToString("o"), "NOTIFY", $"Low balance warning sent to {email} for account {r[0]}", "system", "INFO" });
            }
        }
    }

    public void SendBlockNotification(int accId)
    {
        int custId = -1;
        foreach (var r in _dataRows)
            if ((int)r[0] == accId) { custId = (int)r[1]; break; }
        if (custId == -1) return;

        string email = "";
        foreach (var c in _custRows)
            if ((int)c[0] == custId) { email = (string)c[2]; break; }

        Console.WriteLine($"[NOTIFY] Account {accId} has been blocked – notification sent to {email}");
    }

    // ----------------------------------------------------------------
    // Hilfsmethoden – kein klarer Kontext, woher sie gehören
    // ----------------------------------------------------------------
    private bool CheckIban(string s)
    {
        // Nur Länge und Grundformat – kein Mod-97
        return s != null && s.Length >= 15 && s.Length <= 34;
    }

    // Duplikat von CheckIban unter anderem Namen, entstanden durch Merge
    private bool ValidateIbanFormat(string iban)
    {
        if (iban == null || iban.Length < 15 || iban.Length > 34) return false;
        return true;
    }

    public bool IsValidSwift(string swift)
    {
        if (string.IsNullOrEmpty(swift)) return false;
        return Regex.IsMatch(swift, @"^[A-Z]{6}[A-Z0-9]{2}([A-Z0-9]{3})?$");
    }

    private string PadLeft(string s, int len) => s?.PadLeft(len, '0') ?? new string('0', len);

    // Nicht verwendet, aber niemand traut sich, es zu löschen
    // private void RecalculateAllBalances() { ... }

    public int GetTotalAccountCount()
    {
        int n = 0;
        foreach (var r in _dataRows)
            if ((string)r[5] != "CLOSED") n++;
        return n;
    }

    public decimal GetTotalBalanceByCurrency(string currency)
    {
        decimal total = 0m;
        foreach (var r in _dataRows)
            if ((string)r[4] == currency && (string)r[5] != "CLOSED")
                total += (decimal)r[3];
        return total;
    }
}
