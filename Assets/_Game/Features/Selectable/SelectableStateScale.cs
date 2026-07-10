using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.Client.UI.Components
{
    [RequireComponent(typeof(ISelectable))]
    public class SelectableStateScale : MonoBehaviour
    {
        [SerializeField] private Transform transformToScale;
        [SerializeField] private float scaleDuration = 0.2f;
        [FormerlySerializedAs("selectionStateToTextColor")] [SerializeField] private SelectionStateToScale selectionState;

        private Vector3 _awakeScale;
        private ISelectable _selectable;

        private void OnValidate()
        {
            if (transformToScale == null) transformToScale = transform;
        }

        private void Awake()
        {
            _awakeScale = transformToScale.localScale;
        }

        private void OnEnable()
        {
            _selectable ??= GetComponent<ISelectable>();
            _selectable.SelectionStateChanged += OnSelectionStateTransition;
        }

        private void OnDisable()
        {
            if (_selectable != null) _selectable.SelectionStateChanged -= OnSelectionStateTransition;
            this.DOComplete();
        }

        public void OnSelectionStateTransition(SelectionState selectionState)
        {
            if (!enabled) return;
            transform.DOKill();
            var scale = this.selectionState.Get(selectionState);
            transformToScale.DOScale(_awakeScale * scale, scaleDuration).SetTarget(transform);
        }

        [Serializable]
        public class SelectionStateToScale
        {
            [SerializeField] private SelectionStateValues<float> values = new()
            {
                Normal = 1f,
                Highlighted = 1.1f,
                Pressed = 0.9f,
                Selected = 1f,
                Disabled = 1f,
            };

            public float Get(SelectionState state) => values.Get(state);
            public void SetNormal(float value) => values.SetNormal(value);
        }
    }
}
