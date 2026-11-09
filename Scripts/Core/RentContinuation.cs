using System;

/// <summary>A stateless rule for bills after a finite contract; null ends the schedule.</summary>
public interface IRentContinuation
{
    Bill? NextAfter(Bill previous);
}

public sealed class CompoundingRent : IRentContinuation
{
    private readonly int _interval, _percent, _minimum, _roundTo;
    public CompoundingRent(int interval, int growthPercent, int minimumIncrease, int roundTo = 1)
    {
        if (interval <= 0 || growthPercent < 0 || growthPercent > 1000 || minimumIncrease <= 0 || roundTo <= 0)
            throw new ArgumentException("Renewal intervals, minimum increases and rounding must be positive; growth must be 0–1000 percent.");
        _interval = interval; _percent = growthPercent; _minimum = minimumIncrease; _roundTo = roundTo;
    }
    public Bill? NextAfter(Bill previous)
    {
        ArgumentNullException.ThrowIfNull(previous);
        if (previous.Shift <= 0 || previous.Amount < 0) throw new ArgumentException("Previous bill must be valid.");
        if (previous.Shift > int.MaxValue - _interval) return null;
        long increase = Math.Max(_minimum, ((long)previous.Amount * _percent + 99) / 100);
        long amount = ((previous.Amount + increase + _roundTo - 1) / _roundTo) * _roundTo;
        return new(previous.Shift + _interval, (int)Math.Min(int.MaxValue, amount));
    }
}
