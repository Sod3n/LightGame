using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.Client.UI.Components
{
    [Serializable]
    public struct SelectionStateValues<T>
    {
        public T Normal;
        public T Highlighted;
        public T Pressed;
        public T Selected;
        public T Disabled;
            
        public T Get(SelectionState state) => state switch
        {
            SelectionState.Normal => Normal,
            SelectionState.Highlighted => Highlighted,
            SelectionState.Pressed => Pressed,
            SelectionState.Selected => Selected,
            SelectionState.Disabled => Disabled,
            SelectionState.DisabledHighlighted => Disabled,
            SelectionState.DisabledPressed => Disabled,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        public void SetNormal(T value) => Normal = value;
    }
}