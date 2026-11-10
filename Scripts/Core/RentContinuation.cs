using System;

/// <summary>
/// 可选的续租规则。返回的账单必须晚于上一笔且金额非负；返回空表示不再续租。
/// </summary>
public interface IRentContinuation
{
    Bill? NextAfter(Bill previous);
}

/// <summary>
/// 按间隔、涨幅、最低增量和取整单位生成续租账单；极端金额饱和，日期溢出时结束。
/// </summary>
public sealed class CompoundingRent : IRentContinuation
{
    private readonly int _interval;
    private readonly int _percent;
    private readonly int _minimum;
    private readonly int _roundTo;

    public CompoundingRent(int interval, int growthPercent, int minimumIncrease, int roundTo = 1)
    {
        if (interval <= 0 || growthPercent < 0 || growthPercent > 1000 || minimumIncrease <= 0 || roundTo <= 0)
        {
            throw new ArgumentException("Renewal intervals, minimum increases and rounding must be positive; growth must be 0–1000 percent.");
        }

        _interval = interval;
        _percent = growthPercent;
        _minimum = minimumIncrease;
        _roundTo = roundTo;
    }

    public Bill? NextAfter(Bill previous)
    {
        ArgumentNullException.ThrowIfNull(previous);
        if (previous.Shift <= 0 || previous.Amount < 0)
        {
            throw new ArgumentException("Previous bill must be valid.");
        }

        if (previous.Shift > int.MaxValue - _interval)
        {
            return null;
        }

        long increase = Math.Max(_minimum, ((long)previous.Amount * _percent + 99) / 100);
        long amount = ((previous.Amount + increase + _roundTo - 1) / _roundTo) * _roundTo;
        return new(previous.Shift + _interval, (int)Math.Min(int.MaxValue, amount));
    }
}
