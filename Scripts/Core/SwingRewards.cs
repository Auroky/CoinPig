using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 一个奖励策略计算出的金币与体力返还，不直接修改钱包。
/// </summary>
public readonly record struct SwingPayout(int Coins, int StaminaRefund);

/// <summary>
/// 无状态奖励策略。只读取攻击快照并使用提供的预算，必须返回非负收益，不能修改目标。
/// </summary>
public interface ISwingRewardRule
{
    SwingPayout Calculate(StrikeSummary strike, Bonus bonus, IRewardBudget budget);
}

/// <summary>
/// 单次试算可使用的限额能力；策略只消费命名预算，不直接访问会话中的已用次数。
/// </summary>
public interface IRewardBudget
{
    int Consume(string pool, int requested, int dailyLimit);
}

/// <summary>
/// 组合奖励策略并管理每日次数。试算使用预算副本，整次攻击成功后才提交使用量。
/// </summary>
public sealed class SwingRewards
{
    private readonly IReadOnlyList<ISwingRewardRule> _rules;
    private Dictionary<string, int> _used = new();

    public SwingRewards(IEnumerable<ISwingRewardRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var snapshot = rules.ToArray();
        if (snapshot.Any(rule => rule == null))
        {
            throw new ArgumentException("Reward rules cannot be null.", nameof(rules));
        }

        _rules = Array.AsReadOnly(snapshot);
    }

    public void BeginDay() => _used.Clear();

    /// <summary>
    /// 待提交的奖励金额和预算副本，供会话完成所有验证后一次性应用。
    /// </summary>
    internal sealed record Quote(SwingPayout Payout, Dictionary<string, int> Usage);

    private sealed class Budget : IRewardBudget
    {
        public Dictionary<string, int> Usage { get; }

        public Budget(Dictionary<string, int> previous) => Usage = new(previous);

        public int Consume(string pool, int requested, int dailyLimit)
        {
            if (string.IsNullOrWhiteSpace(pool) || requested < 0 || dailyLimit < 0)
            {
                throw new ArgumentException("Reward budgets need a key and nonnegative limits.");
            }

            int used = Usage.GetValueOrDefault(pool);
            int granted = Math.Min(requested, Math.Max(0, dailyLimit - used));
            Usage[pool] = checked(used + granted);
            return granted;
        }
    }

    /// <summary>在预算副本中试算所有奖励；策略失败或数值溢出时不消耗真实预算。</summary>
    internal Quote Prepare(SwingResult strike, Bonus bonus, int staminaSpent = 1)
    {
        var budget = new Budget(_used);
        var summary = new StrikeSummary(strike, staminaSpent);
        int coins = strike.BaseCoins;
        int refunds = 0;
        foreach (var rule in _rules)
        {
            var payout = rule.Calculate(summary, bonus, budget);
            if (payout.Coins < 0 || payout.StaminaRefund < 0)
            {
                throw new InvalidOperationException("A reward rule returned a negative payout.");
            }

            coins = checked(coins + payout.Coins);
            refunds = checked(refunds + payout.StaminaRefund);
        }
        return new(new(coins, refunds), budget.Usage);
    }

    internal void Commit(Quote quote) => _used = quote.Usage;
}
