using Godot;
using System;

/// <summary>Optional presentation feedback; coin arrival never changes the balance.</summary>
public interface IRewardFeedback
{
    Vector2 CoinDestination { get; set; }
    event Action? CoinCollected;
}
