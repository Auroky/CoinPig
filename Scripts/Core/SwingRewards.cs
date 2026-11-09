using System;
using System.Collections.Generic;
using System.Linq;

public readonly record struct SwingPayout(int Coins, int StaminaRefund);

/// <summary>Rules are stateless, do not mutate targets, return nonnegative rewards and use the supplied budget.</summary>
public interface ISwingRewardRule
{
    SwingPayout Calculate(StrikeSummary strike, Bonus bonus, IRewardBudget budget);
}
public interface IRewardBudget
{
    int Consume(string pool, int requested, int dailyLimit);
}

/// <summary>Composes reward policies and owns per-session, per-day usage limits.</summary>
public sealed class SwingRewards
{
    private readonly IReadOnlyList<ISwingRewardRule> _rules;
    private Dictionary<string, int> _used = new();
    public SwingRewards(IEnumerable<ISwingRewardRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var snapshot = rules.ToArray();
        if (snapshot.Any(rule => rule == null)) throw new ArgumentException("Reward rules cannot be null.", nameof(rules));
        _rules = Array.AsReadOnly(snapshot);
    }
    public void BeginDay() => _used.Clear();
    internal sealed record Quote(SwingPayout Payout, Dictionary<string, int> Usage);
    private sealed class Budget : IRewardBudget
    {
        public Dictionary<string, int> Usage { get; }
        public Budget(Dictionary<string, int> previous) => Usage = new(previous);
        public int Consume(string pool, int requested, int dailyLimit)
        {
            if (string.IsNullOrWhiteSpace(pool) || requested < 0 || dailyLimit < 0)
                throw new ArgumentException("Reward budgets need a key and nonnegative limits.");
            int used = Usage.GetValueOrDefault(pool);
            int granted = Math.Min(requested, Math.Max(0, dailyLimit - used));
            Usage[pool] = checked(used + granted);
            return granted;
        }
    }
    internal Quote Prepare(SwingResult strike, Bonus bonus)
    {
        var budget = new Budget(_used);
        var summary = new StrikeSummary(strike);
        int coins = strike.BaseCoins, refunds = 0;
        foreach (var rule in _rules)
        {
            var payout = rule.Calculate(summary, bonus, budget);
            if (payout.Coins < 0 || payout.StaminaRefund < 0)
                throw new InvalidOperationException("A reward rule returned a negative payout.");
            coins = checked(coins + payout.Coins);
            refunds = checked(refunds + payout.StaminaRefund);
        }
        return new(new(coins, refunds), budget.Usage);
    }
    internal void Commit(Quote quote) => _used = quote.Usage;
}
