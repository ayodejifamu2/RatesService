namespace RatesService.Domain.ValueObjects;

public class Money : IEquatable<Money>
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    
    private Money() { }
    
    public Money(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency cannot be empty.", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant(); // Standardize currency
    }
    
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException("Cannot add Money with different currencies.");
        }
        return new Money(Amount + other.Amount, Currency);
    }
    
    public Money Multiply(decimal factor)
    {
        return new Money(Amount * factor, Currency);
    }
    
    public override bool Equals(object obj)
    {
        return Equals(obj as Money);
    }
    
    public bool Equals(Money other)
    {
        return other != null! &&
               Amount == other.Amount &&
               Currency == other.Currency;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Amount, Currency);
    }

    public static bool operator ==(Money left, Money right)
    {
        return EqualityComparer<Money>.Default.Equals(left, right);
    }

    public static bool operator !=(Money left, Money right)
    {
        return !(left == right);
    }
}