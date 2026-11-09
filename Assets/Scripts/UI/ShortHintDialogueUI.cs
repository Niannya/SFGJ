using PixelCrushers.DialogueSystem.Wrappers;
using UnityEngine;

/// <summary>Keeps game-wide hint alerts brief and replaces stale hints instead of queueing them.</summary>
public sealed class ShortHintDialogueUI : StandardDialogueUI
{
    private const float MaxHintSeconds = 2f;

    public override void Awake()
    {
        alertUIElements.queueAlerts = false;
        base.Awake();
    }

    public override void ShowAlert(string message, float duration)
    {
        base.ShowAlert(message, Mathf.Min(duration, MaxHintSeconds));
    }
}
