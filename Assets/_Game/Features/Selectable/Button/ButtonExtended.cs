using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.Client.UI.Components
{
    public class ButtonExtended : Button, ISelectable
    {
        public event Action<Components.SelectionState> SelectionStateChanged;
        public Components.SelectionState CurrentState { get; private set; } = Components.SelectionState.Normal;

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            EventSystem.current.SetSelectedGameObject(null);
        }

        protected override void DoStateTransition(Selectable.SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            if (!gameObject.activeInHierarchy) return;
            if (!Application.isPlaying) return;

            var newState = (Components.SelectionState)state;
            if (newState == CurrentState) return;
            CurrentState = newState;
            SelectionStateChanged?.Invoke(newState);
        }
    }
}
