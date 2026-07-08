using System;
using UnityEngine;
using UnityEngine.UI;
using R3;
using UltEvents;

namespace Core.Client.UI.Components
{
    [RequireComponent(typeof(ISelectable))]
    public class SelectableStateColor : MonoBehaviour
    {
        [SerializeField] private UltEvent<Color> onChange;
        [SerializeField] private SelectionStateToSprite selectionStateToSprite;
        
        private void Awake()
        {
            var button = GetComponent<ISelectable>();
            button.SelectionStateTransition.Subscribe(OnSelectionStateTransition).AddTo(this);
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
                // DisabledPressed = Color.white,
                // DisabledHighlighted = Color.white
            };
    
            public Color Get(SelectionState state) => values.Get(state);
            public void SetNormal(Color value) => values.SetNormal(value);
        }
    }
}