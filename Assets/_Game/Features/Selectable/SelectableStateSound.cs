using System;
using LightGame.Audio;
using UnityEngine;
using UnityEngine.UI;
using R3;
using UltEvents;

namespace Core.Client.UI.Components
{
    [RequireComponent(typeof(ISelectable))]
    public class SelectableStateSound : MonoBehaviour
    {
        [SerializeField] private SelectionStateToSound selectionStateToSprite;
        
        private void Awake()
        {
            var button = GetComponent<ISelectable>();
            button.SelectionStateTransition.Subscribe(OnSelectionStateTransition).AddTo(this);
        }
        
        public void OnSelectionStateTransition(SelectionState selectionState)
        {
            var soundData = selectionStateToSprite.Get(selectionState);
            soundData?.Play();
        }
        
        [Serializable]
        public class SelectionStateToSound
        {
            [SerializeField] private SelectionStateValues<SoundData> values = new()
            {
            };
    
            public SoundData Get(SelectionState state) => values.Get(state);
            public void SetNormal(SoundData value) => values.SetNormal(value);
        }
    }
}