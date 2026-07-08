using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Light_and_controller.Scripts
{
    public class Root : MonoBehaviour
    {
        private void Awake()
        {
            ExecuteEvents.ExecuteHierarchy<IInitializable>(gameObject, null, (x, _) => x.Initialize());
        }
    }
}