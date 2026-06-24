namespace CvSU.Ais.Domain.Common;

/// <summary>
/// Philippine Peso amount, stored to centavo precision.
///
/// A value object: two <see cref="Money"/> with the same amount are equal.
/// Government accounting is unforgiving about rounding, so every amount is
/// rounded to two decimals on construction and all arithmetic stays in
/// <see cref="decimal"/> — never <see cref="double"/>/<see cref="float"/>.
/// </summary>
public readonly record struct Money : IComparable<Money>
{
    /// <summary>Tolerance for "balanced" comparisons (R-GL-01): one centavo.</summary>
    public const decimal Tolerance = 0.01m;

    public decimal Amount { get; }

    public Money(decimal amount)
    {
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    public static Money Zero => new(0m);

    public bool IsZero => Amount == 0m;
    public bool IsPositive => Amount > 0m;
    public bool IsNegative => Amount < 0m;

    public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount);
    public static Money operator -(Money a, Money b) => new(a.Amount - b.Amount);

    public static bool operator >(Money a, Money b) => a.Amount > b.Amount;
    public static bool operator <(Money a, Money b) => a.Amount < b.Amount;
    public static bool operator >=(Money a, Money b) => a.Amount >= b.Amount;
    public static bool operator <=(Money a, Money b) => a.Amount <= b.Amount;

    /// <summary>True when the two amounts differ by no more than one centavo.</summary>
    public bool EqualsWithinTolerance(Money other) => Math.Abs(Amount - other.Amount) <= Tolerance;

    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);

    public override string ToString() => $"₱{Amount:N2}";
}
