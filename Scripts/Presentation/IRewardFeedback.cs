using Godot;
using System;

/// <summary>
/// 金币飞行动画的可选能力；到账事件仅用于界面反馈，不用于增加钱包数值。
/// </summary>
public interface IRewardFeedback
{
    Vector2 CoinDestination { get; set; }
    event Action? CoinCollected;
}
