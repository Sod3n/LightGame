using System;
using System.Collections.Generic;
using Light_and_controller.Scripts.Systems;
using UnityEngine;
using UnityEngine.Events;

namespace Light_and_controller.Scripts.Components
{
    public class WeightTrigger : MonoBehaviour
    {
        [Header("Mode")]
        [SerializeField] private bool toggleMode = false;
        [SerializeField] private bool interactMode = false;
        
        [Header("Interact Mode Settings")]
        [SerializeField] private KeyCode interactKey = KeyCode.F;
        [SerializeField] private string playerTag = "Player";
        
        [Header("Weight Mode Settings")]
        [SerializeField] private float weightThreshold;
        [SerializeField] private bool canDeactivate = true;
        
        [Header("Initial State")]
        [SerializeField] private bool startActivated = false;
        
        [Header("References")]
        [SerializeField] private List<Togglable> togglables = new List<Togglable>();
        public UnityEvent onActivate = new UnityEvent();
        public UnityEvent onDeactivate = new UnityEvent();
        
        private float _currentWeight;
        private bool _active;
        private bool _playerInZone;

        private HashSet<GameObject> AddedWeights { get; } = new();
        
        public bool IsActive => _active;

        private void Start()
        {
            // Initialize based on startActivated setting
            if (startActivated)
            {
                _active = true;
                ActivateTogglables();
                onActivate.Invoke();
            }
            else
            {
                _active = false;
                DeactivateTogglables();
                onDeactivate.Invoke();
            }
        }

        private void Update()
        {
            if (interactMode && _playerInZone && Input.GetKeyDown(interactKey))
            {
                ToggleState();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (interactMode)
            {
                // Interact mode: track player presence
                if (other.CompareTag(playerTag))
                {
                    _playerInZone = true;
                }
            }
            else if (toggleMode)
            {
                // Toggle mode: each enter toggles the state
                ToggleState();
            }
            else
            {
                // Weight mode: accumulate weight
                if(!AddedWeights.Add(other.gameObject)) return;
                var weightRequest = new WeightRequestEvent();
                EventBus.Publish(other.gameObject, weightRequest);
                if (weightRequest.Weight > 0)
                {
                    AddWeight(weightRequest.Weight);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (interactMode)
            {
                // Interact mode: track player leaving
                if (other.CompareTag(playerTag))
                {
                    _playerInZone = false;
                }
            }
            else if (toggleMode)
            {
                // Toggle mode: exit does nothing (or could toggle again if desired)
                return;
            }
            else
            {
                // Weight mode: remove weight
                if(!AddedWeights.Remove(other.gameObject)) return;
                var weightRequest = new WeightRequestEvent();
                EventBus.Publish(other.gameObject, weightRequest);
                if (weightRequest.Weight > 0)
                {
                    RemoveWeight(weightRequest.Weight);
                }
            }
        }

        private void AddWeight(float value)
        {
            _currentWeight += value;
            if (!(_currentWeight >= weightThreshold) || _active) return;
            
            onActivate.Invoke();
            ActivateTogglables();
            _active = true;
        }

        private void RemoveWeight(float value)
        {
            _currentWeight -= value;
            if (!(_currentWeight < weightThreshold) || !_active || !canDeactivate) return;
            
            onDeactivate.Invoke();
            DeactivateTogglables();
            _active = false;
        }

        private void ActivateTogglables()
        {
            foreach (var togglable in togglables)
            {
                if (togglable != null)
                {
                    togglable.Enable();
                }
            }
        }

        private void DeactivateTogglables()
        {
            foreach (var togglable in togglables)
            {
                if (togglable != null)
                {
                    togglable.Disable();
                }
            }
        }

        private void ToggleState()
        {
            if (_active)
            {
                onDeactivate.Invoke();
                DeactivateTogglables();
                _active = false;
            }
            else
            {
                onActivate.Invoke();
                ActivateTogglables();
                _active = true;
            }
        }
    }
}