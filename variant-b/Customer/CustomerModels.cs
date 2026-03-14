namespace BankApp.Customer;

// ============================================================
// Domain Models – Bounded Context: Customer
// ============================================================

/// <summary>
/// Bankkundenprofil mit Identifikations- und Kontaktdaten.
/// </summary>
public record Customer(
    int CustomerId,
    string FullName,
    string Email,
    bool IsActive
);

/// <summary>
/// Repository-Abstraktion für Kundendaten.
/// </summary>
public interface ICustomerRepository
{
    Customer? FindById(int customerId);
    IReadOnlyList<Customer> FindAllActive();
}

/// <summary>
/// In-Memory-Implementierung für PoC.
/// </summary>
public class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly Dictionary<int, Customer> _customers = new()
    {
        { 1, new Customer(1, "Hans Muster",   "hans.muster@example.com",   true)  },
        { 2, new Customer(2, "Maria Müller",  "maria.mueller@example.com", true)  },
        { 3, new Customer(3, "Peter Schmid",  "peter.schmid@example.com",  false) },
    };

    public Customer? FindById(int customerId) =>
        _customers.TryGetValue(customerId, out var c) ? c : null;

    public IReadOnlyList<Customer> FindAllActive() =>
        _customers.Values.Where(c => c.IsActive).ToList();
}
