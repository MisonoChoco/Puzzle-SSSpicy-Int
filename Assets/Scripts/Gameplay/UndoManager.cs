using UnityEngine;
using System;

public static class UndoSystem
{
    public static event Action OnUndoPressed;

    public static void TriggerUndo()
    {
        OnUndoPressed?.Invoke();
    }
}