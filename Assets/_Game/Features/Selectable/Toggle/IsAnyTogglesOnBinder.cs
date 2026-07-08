using System;
using MVVM.Binders;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Client.UI.Components
{
    [RequireComponent(typeof(Toggle))]
    public class IsAnyTogglesOnBinder : ObservableBinder<bool>
    {
        private Toggle _toggle;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
        }

        private void Update()
        {
            if(_toggle.group == null) return;
            var newValue = _toggle.group.AnyTogglesOn();
            if(newValue == Value) return;
            Value = newValue;
        }
    }
}