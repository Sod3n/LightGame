using System;
using MVVM.Binders;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Client.UI.Components
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleStateBinder : ObservableBinder<bool>
    {
        private Toggle _toggle;

        private void OnEnable()
        {
            _toggle ??= GetComponent<Toggle>();
            _toggle.onValueChanged.AddListener(SetValue);
        }

        private void OnDisable()
        {
            _toggle.onValueChanged.RemoveListener(SetValue);
        }

        private void SetValue(bool value) => Value = value;
    }
}