using System;
using Light_and_controller.Scripts.Systems;
using UnityEngine;
using UnityEngine.Serialization;

namespace Light_and_controller.Scripts.Components
{
    public class Weight : MonoBehaviour, IWeight
    {
        [SerializeField] private ObjectsWeight.Type type;
        [FormerlySerializedAs("weight")] [SerializeField] private float fallback;
        

        private void OnEnable()
        {
            EventBus.Subscribe<WeightRequestEvent>(gameObject, OnWeightRequest);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<WeightRequestEvent>(gameObject, OnWeightRequest);
        }

        private void OnWeightRequest(WeightRequestEvent evt)
        {
            evt.Weight = GD.ObjectsWeight.GetWeight(type);
        }
        
        public float Get()
        {
            return GD.ObjectsWeight.GetWeight(type);
        }
    }
}