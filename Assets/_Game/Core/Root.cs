using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LightGame.Core
{
    public class Root : MonoBehaviour
    {
        private void Awake()
        {
            ExecuteEvents.ExecuteHierarchy<IInitializable>(gameObject, null, (x, _) => x.Initialize());
        }
    }
}