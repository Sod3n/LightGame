
using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class AutoBindToggleGroup : MonoBehaviour
{
    private Toggle _toggle;

    private void Start()
    {
        _toggle ??= GetComponent<Toggle>();
        _toggle.group = GetComponentInParent<ToggleGroup>();
    }
}