using System;
using DG.Tweening;
using Plugins.MVVM.Runtime;
using R3;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.Client.UI.Components
{
    [RequireComponent(typeof(ISelectable))]
    public class SelectableStateSorting: MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private SelectionStateToSorting selectionState;

        private Vector3 _awakeScale;
        
        private void OnValidate()
        {
            if (canvas == null) canvas = GetComponentInChildren<Canvas>();
        }
        
        private void Awake()
        {
            var button = GetComponent<ISelectable>();
            button.SelectionStateTransition.Subscribe(OnSelectionStateTransition).AddTo(this);
        }

        private void OnDisable()
        {
            this.DOComplete();
        }

        public void OnSelectionStateTransition(SelectionState selectionState)
        {
            if(!enabled) return;
            var sorting = this.selectionState.Get(selectionState);
            if(!sorting.Enabled) return;
            canvas.overrideSorting = sorting.OverrideSorting;
            canvas.sortingLayerID = sorting.SortingLayerID;
            canvas.sortingOrder = sorting.SortingOrder;
        }

        [Serializable]
        public class Sorting
        {
            public bool Enabled;
            public bool OverrideSorting;
            [SortingLayer] public int SortingLayerID;
            public int SortingOrder;
        }
        
        [Serializable]
        public class SelectionStateToSorting
        {
            [SerializeField] private SelectionStateValues<Sorting> values = new()
            {
            };
    
            public Sorting Get(SelectionState state) => values.Get(state);
            public void SetNormal(Sorting value) => values.SetNormal(value);
        }
    }
}