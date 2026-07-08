using System;
using System.Collections.Generic;
using Light_and_controller.Scripts.Components;
using UnityEngine;

namespace Light_and_controller.Scripts
{
    [CreateAssetMenu(fileName = "ObjectsWeight", menuName = "Game Data/ObjectsWeight")]
    public class ObjectsWeight : ScriptableObject
    {
        public enum Type
        {
            Player,
            MovableCube
        }

        [SerializeField] private List<Pair> values = new();

        public List<Pair> Values => values;
        
        public float GetWeight(ObjectsWeight.Type type)
        {
            return values.Find(x => x.Type == type).Weight;
        }
        
        [Serializable]
        public class Pair
        {
            public ObjectsWeight.Type Type;
            public float Weight;
        }
    }
}