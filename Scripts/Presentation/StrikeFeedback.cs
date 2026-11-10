using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 已结算攻击的表现观察者；不能再次发放金币或修改目标耐久。
/// </summary>
public interface IStrikeFeedback
{
    void Present(StrikeOutcome outcome);
}

/// <summary>
/// 把单个表现回调适配为攻击反馈观察者，便于在场景入口组合已有组件。
/// </summary>
public sealed class ActionStrikeFeedback : IStrikeFeedback
{
    private readonly Action<StrikeOutcome> _present;

    public ActionStrikeFeedback(Action<StrikeOutcome> present)
    {
        ArgumentNullException.ThrowIfNull(present);
        _present = present;
    }

    public void Present(StrikeOutcome outcome) => _present(outcome);
}

/// <summary>
/// 按注册顺序通知多个表现观察者，保存独立的观察者列表快照。
/// </summary>
public sealed class CompositeStrikeFeedback : IStrikeFeedback
{
    private readonly IReadOnlyList<IStrikeFeedback> _observers;

    public CompositeStrikeFeedback(IEnumerable<IStrikeFeedback> observers)
    {
        ArgumentNullException.ThrowIfNull(observers);
        var snapshot = observers.ToArray();
        if (snapshot.Any(observer => observer == null))
        {
            throw new ArgumentException("Feedback observers cannot be null.", nameof(observers));
        }

        _observers = Array.AsReadOnly(snapshot);
    }

    public void Present(StrikeOutcome outcome)
    {
        foreach (var observer in _observers)
        {
            observer.Present(outcome);
        }
    }
}
