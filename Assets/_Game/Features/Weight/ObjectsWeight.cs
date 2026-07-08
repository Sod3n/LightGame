using System;
using System.Collections.Generic;
using LightGame.Features;
using UnityEngine;

namespace LightGame
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