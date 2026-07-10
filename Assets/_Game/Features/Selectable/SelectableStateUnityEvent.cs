using System;
using UltEvents;
using UnityEngine;

namespace Core.Client.UI.Components
{
    public class SelectableStateUnityEvent : MonoBehaviour
    {
        [SerializeField] private SelectionStateToUnityEvent selectionStateToUnityEvent;

        private UltEvent<bool> _previous;
        private ISelectable _selectable;

        private void OnEnable()
        {
            _selectable ??= GetComponentInParent<ISelectable>();
            _selectable.SelectionStateChanged += OnSelectionStateTransition;
        }

        private void OnDisable()
        {
            if (_selectable != null) _selectable.SelectionStateChanged -= OnSelectionStateTransition;
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
