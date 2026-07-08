using System;
using R3;
using TMPro;
using UnityEngine;

namespace Core.Client.UI.Components
{
    [RequireComponent(typeof(ISelectable))]
    public class SelectableStateTextMaterial : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private bool changeMaterial;
        [SerializeField] private SelectionStateToTextMaterial selectionStateToTextMaterial;
        [SerializeField] private bool changeColor;
        [SerializeField] private SelectionStateToTextColor selectionStateToTextColor;
        
        public void Awake()
        {
            selectionStateToTextMaterial.SetNormal(text.material);
            selectionStateToTextColor.SetNormal(text.color);
            
            var button = GetComponent<ISelectable>();
            button.SelectionStateTransition.Subscribe(OnSelectionStateTransition).AddTo(this);
        }

        public void OnSelectionStateTransition(SelectionState selectionState)
        {
            if (changeMaterial) ChangeMaterial(selectionState);
            if (changeColor) ChangeColor(selectionState);
        }

        private void ChangeMaterial(SelectionState selectionState)
        {
            text.material = selectionStateToTextMaterial.Get(selectionState);

        }

        private void ChangeColor(SelectionState selectionState)
        {
            text.color = selectionStateToTextColor.Get(selectionState);
        }
        
        [Serializable]
        public class SelectionStateToTextMaterial
        {
            [SerializeField] private SelectionStateValues<Material> values = new();
            
            public Material Get(SelectionState state) => values.Get(state);
            public void SetNormal(Material value) => values.SetNormal(value);
        }
        
        [Serializable]
        public class SelectionStateToTextColor
        {
            [SerializeField] private SelectionStateValues<Color> values = new()
            {
                Normal = Color.white,
                Highlighted = Color.white,
                Pressed = Color.white,
                Selected = Color.white,
                Disabled = Color.white
            };
    
            public Color Get(SelectionState state) => values.Get(state);
            public void SetNormal(Color value) => values.SetNormal(value);
        }
    }
}