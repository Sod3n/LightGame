using System;
using MVVM.Binders;
using UnityEngine;
using UnityEngine.UI;
using R3;

namespace Core.Client.UI.Components
{
    [RequireComponent(typeof(ButtonExtended))]
    public class ButtonStateBinder : GenericMethodBinder<SelectionState>
    {
        private void Awake()
        {
            var button = GetComponent<ButtonExtended>();
            button.SelectionStateTransition.Subscribe(OnSelectionStateTransition).AddTo(this);
        }
        
        public void OnSelectionStateTransition(SelectionState selectionState)
        {
            Perform(selectionState);
        }
    }
}