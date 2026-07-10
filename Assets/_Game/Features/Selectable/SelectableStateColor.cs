using System;
using UltEvents;
using UnityEngine;

namespace Core.Client.UI.Components
{
    [RequireComponent(typeof(ISelectable))]
    public class SelectableStateColor : MonoBehaviour
    {
        [SerializeField] private UltEvent<Color> onChange;
        [SerializeField] private SelectionStateToSprite selectionStateToSprite;

        private ISelectable _selectable;

        private void OnEnable()
        {
            _selectable ??= GetComponent<ISelectable>();
            _selectable.SelectionStateChanged += OnSelectionStateTransition;
            OnSelectionStateTransition(_selectable.CurrentState);
        }

        private void OnDisable()
        {
            if (_selectable != null) _selectable.SelectionStateChanged -= OnSelectionStateTransition;
        }

        public void OnSelectionStateTransition(SelectionState selectionState)
        {
            var color = selectionStateToSprite.Get(selectionState);
            onChange.Invoke(color);
        }

        [Serializable]
        public class SelectionStateToSprite
        {
            [SerializeField] private SelectionStateValues<Color> values = new()
            {
                Normal = Color.white,
                Highlighted = Color.white,
                Pressed = Color.white,
                Selected = Color.white,
                Disabled = Color.white,
            };

            public Color Get(SelectionState state) => values.Get(state);
            public void SetNormal(Color value) => values.SetNormal(value);
        }
    }
}
