using System;
using LightGame.Globals;
using UnityEngine;
using UnityEngine.Serialization;

using LightGame.Core;
namespace LightGame.Features
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
            evt.Weight = Game.ObjectsWeight.GetWeight(type);
        }
        
        public float Get()
        {
            return Game.ObjectsWeight.GetWeight(type);
        }
    }
}