using System;
using R3;
using UltEvents;
using UnityEngine;

namespace Core.Client.UI.Components
{
    public class SelectableStateSeparateUnityEvent : MonoBehaviour
    {
        [SerializeField] private SelectionStateToUnityEvent onEnter;
        [SerializeField] private SelectionStateToUnityEvent onExit;

        private UltEvent _previous;
        private ISelectable selectable;
        
        private void Awake()
        {
            selectable ??= GetComponentInParent<ISelectable>();
            selectable.SelectionStateTransition.Subscribe(OnSelectionStateTransition).AddTo(this);
        }
        
        public void OnSelectionStateTransition(SelectionState selectionState)
        {
            var enterEvent = onEnter.Get(selectionState);
            var exitEvent = onExit.Get(selectionState);
            _previous?.Invoke();
            enterEvent.Invoke();
            _previous = exitEvent;
        }
        
        [Serializable]
        public class SelectionStateToUnityEvent
        {
            [SerializeField] private SelectionStateValues<UltEvent> values = new()
            {
                Normal = null,
                Highlighted = null,
                Pressed = null,
                Selected = null,
                Disabled = null
            };
    
            public UltEvent Get(SelectionState state) => values.Get(state);
            public void SetNormal(UltEvent value) => values.SetNormal(value);
        }
    }
}