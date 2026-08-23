using System;

namespace Core.Client.UI.Components
{
    /// <summary>
    /// Selectables (Button, Toggle, etc.) publish selection-state transitions via a plain C# event.
    /// Consumers subscribe in OnEnable and unsubscribe in OnDisable.
    /// </summary>
    public interface ISelectable
    {
        event Action<SelectionState> SelectionStateChanged;
        SelectionState CurrentState { get; }
    }
}
