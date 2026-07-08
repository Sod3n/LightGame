using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Light_and_controller.Scripts.Components
{
    public abstract class MonoBehaviourEffect<T> : MonoBehaviourWithData<T> where T : EffectData
    {
        protected float _currentTime;
        protected float _timeSinceLastTick;
        private bool _hasStarted;

        private void Update()
        {
            if (!_hasStarted)
            {
                OnStart();
                _hasStarted = true;
            }

            _timeSinceLastTick += Time.deltaTime;
            _currentTime += Time.deltaTime;

            if (_timeSinceLastTick >= Data.Rate)
            {
                Tick();
                _timeSinceLastTick = 0f;
            }

            if (!Data.IsInfinity && _currentTime >= Data.Duration)
            {
                Destroy(this);
            }
        }

        protected virtual void OnStart() { }
        protected abstract void Tick();
    }
    
    [Serializable]
    public class EffectData
    {
        public float Duration;
        public float Rate;
        public bool IsInfinity;
    }
}