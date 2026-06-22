using HomeProjectManagement.Domain.Common;

namespace HomeProjectManagement.Domain.Common.ValueObjects;

/// <summary>
/// A value-added-tax rate, expressed as a percentage (e.g. <c>21</c> means 21%). Immutable. The
/// Romanian standard rate of 21% is the <see cref="Standard"/> default applied to bill-of-quantities
/// line items unless a different rate is given; <c>0</c> models a VAT-exempt line. Applying the rate
/// to a net (VAT-exclusive) <see cref="Money"/> amount yields the gross (VAT-inclusive) amount.
/// </summary>
public sealed class VatRate : ValueObject
{
    /// <summary>The Romanian standard VAT rate (21%), used as the default for line items.</summary>
    public const decimal StandardPercentage = 21m;

    /// <summary>The rate as a percentage: <c>21</c> means 21%, <c>0</c> means VAT-exempt.</summary>
    public decimal Percentage { get; }

    public VatRate(decimal percentage)
    {
        if (percentage < 0m)
        {
            throw new DomainValidationException("VAT rate cannot be negative.", nameof(percentage));
        }

        if (percentage > 100m)
        {
            throw new DomainValidationException("VAT rate cannot exceed 100%.", nameof(percentage));
        }

        Percentage = percentage;
    }

    /// <summary>The Romanian standard rate (21%) — the default applied to line items.</summary>
    public static VatRate Standard => new(StandardPercentage);

    /// <summary>The VAT-exclusive → VAT-inclusive multiplier (e.g. <c>1.21</c> for 21%).</summary>
    public decimal GrossMultiplier => 1m + Percentage / 100m;

    /// <summary>The VAT portion of a net amount (<c>net × rate</c>), in the net amount's currency.</summary>
    public Money VatOn(Money net) => net.Multiply(Percentage / 100m);

    /// <summary>The gross (VAT-inclusive) equivalent of a net amount (<c>net × (1 + rate)</c>).</summary>
    public Money ApplyTo(Money net) => net.Multiply(GrossMultiplier);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Percentage;
    }

    public override string ToString() => $"{Percentage:0.##}%";
}
