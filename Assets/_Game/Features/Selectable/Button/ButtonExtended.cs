using System;
using Plugins.MVVM.Runtime.UIComponents.DragNDrop;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core.Client.UI.Components
{
    public class ButtonExtended : Button, ISelectable
    {
        public ReactiveProperty<Components.SelectionState> SelectionStateTransition { get; } = new();

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        protected override void DoStateTransition(Selectable.SelectionState state, bool instant)
        {
            if(DragManager.IsAnyDragging) return;
            base.DoStateTransition(state, instant);
            
            if (!gameObject.activeInHierarchy)
                return;
            
            if(!Application.isPlaying) return;
            
            SelectionStateTransition.Value = (Components.SelectionState)state;
        }
    }
}