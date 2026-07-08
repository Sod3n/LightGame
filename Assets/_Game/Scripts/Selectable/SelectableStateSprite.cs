using System;
using UnityEngine;
using UnityEngine.UI;
using R3;

namespace Core.Client.UI.Components
{
    [RequireComponent(typeof(ISelectable))]
    public class SelectableStateSprite : MonoBehaviour
    {
        [SerializeField] private Image imageToSwap;
        [SerializeField] private SelectionStateToSprite selectionStateToSprite;
        
        private void OnValidate()
        {
            imageToSwap ??= GetComponent<Image>();
        }
        
        private void Awake()
        {
            var button = GetComponent<ISelectable>();
            selectionStateToSprite.SetNormal(imageToSwap?.sprite);
            button.SelectionStateTransition.Subscribe(OnSelectionStateTransition).AddTo(this);
        }
        
        public void OnSelectionStateTransition(SelectionState selectionState)
        {
            var sprite = selectionStateToSprite.Get(selectionState);
            if (sprite != null && imageToSwap != null)
            {
                imageToSwap.sprite = sprite;
            }
        }
        
        [Serializable]
        public class SelectionStateToSprite
        {
            [SerializeField] private SelectionStateValues<Sprite> values = new()
            {
                Normal = null,
                Highlighted = null,
                Pressed = null,
                Selected = null,
                Disabled = null
            };
    
            public Sprite Get(SelectionState state) => values.Get(state);
            public void SetNormal(Sprite value) => values.SetNormal(value);
        }
    }
}