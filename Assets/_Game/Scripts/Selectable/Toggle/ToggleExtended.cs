using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Client.UI.Components
{
    public class ToggleExtended : Toggle, ISelectable
    {
        public ReactiveProperty<Components.SelectionState> SelectionStateTransition { get; } = new();

        protected override void DoStateTransition(Selectable.SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            
            if (!gameObject.activeInHierarchy)
                return;
            
            if(!Application.isPlaying) return;
            
            SelectionStateTransition.Value = (Components.SelectionState)state;
        }
        
        
    }
}