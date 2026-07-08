using System;
using R3;
using UltEvents;
using UnityEngine;

namespace Core.Client.UI.Components
{
    public class SelectableStateUnityEvent : MonoBehaviour
    {
        [SerializeField] private SelectionStateToUnityEvent selectionStateToUnityEvent;

        private UltEvent<bool> _previous;
        private ISelectable selectable;
        
        private void Awake()
        {
            selectable ??= GetComponentInParent<ISelectable>();
            selectable.SelectionStateTransition.Subscribe(OnSelectionStateTransition).AddTo(this);
        }
        
        public void OnSelectionStateTransition(SelectionState selectionState)
        {
            var ultEvent = selectionStateToUnityEvent.Get(selectionState);
            _previous?.Invoke(false);
            ultEvent.Invoke(true);
            _previous = ultEvent;
        }
        
        [Serializable]
        public class SelectionStateToUnityEvent
        {
            [SerializeField] private SelectionStateValues<UltEvent<bool>> values = new()
            {
                Normal = null,
                Highlighted = null,
                Pressed = null,
                Selected = null,
                Disabled = null
            };
    
            public UltEvent<bool> Get(SelectionState state) => values.Get(state);
            public void SetNormal(UltEvent<bool> value) => values.SetNormal(value);
        }
    }
}