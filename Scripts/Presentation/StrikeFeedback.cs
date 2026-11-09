using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Presentation-only observers; they must never grant currency or damage targets.</summary>
public interface IStrikeFeedback
{
    void Present(StrikeOutcome outcome);
}

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

public sealed class CompositeStrikeFeedback : IStrikeFeedback
{
    private readonly IReadOnlyList<IStrikeFeedback> _observers;
    public CompositeStrikeFeedback(IEnumerable<IStrikeFeedback> observers)
    {
        ArgumentNullException.ThrowIfNull(observers);
        var snapshot = observers.ToArray();
        if (snapshot.Any(observer => observer == null)) throw new ArgumentException("Feedback observers cannot be null.", nameof(observers));
        _observers = Array.AsReadOnly(snapshot);
    }
    public void Present(StrikeOutcome outcome)
    {
        foreach (var observer in _observers) observer.Present(outcome);
    }
}
