using System;
using LightGame.Features.Audio;
using UnityEngine;

namespace Core.Client.UI.Components
{
    [RequireComponent(typeof(ISelectable))]
    public class SelectableStateSound : MonoBehaviour
    {
        [SerializeField] private SelectionStateToSound selectionStateToSprite;

        private ISelectable _selectable;

        private void OnEnable()
        {
            _selectable ??= GetComponent<ISelectable>();
            _selectable.SelectionStateChanged += OnSelectionStateTransition;
        }

        private void OnDisable()
        {
            if (_selectable != null) _selectable.SelectionStateChanged -= OnSelectionStateTransition;
        }

        public void OnSelectionStateTransition(SelectionState selectionState)
        {
            var soundData = selectionStateToSprite.Get(selectionState);
            soundData?.Play();
        }

        [Serializable]
        public class SelectionStateToSound
        {
            [SerializeField] private SelectionStateValues<SoundData> values = new() { };

            public SoundData Get(SelectionState state) => values.Get(state);
            public void SetNormal(SoundData value) => values.SetNormal(value);
        }
    }
}
