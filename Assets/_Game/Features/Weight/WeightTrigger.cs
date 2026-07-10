using System;
using System.Collections.Generic;
using LightGame.Globals;
using UnityEngine;

using LightGame.Core;
namespace LightGame.Features
{
    public class WeightTrigger : MonoBehaviour
    {
        [Header("Mode")]
        [SerializeField, Tooltip("If ON: each entry into the trigger flips the state (on/off/on/off). Ignores weight.")]
        private bool toggleMode = false;
        [SerializeField, Tooltip("If ON: player must press the interact key while inside the trigger. Ignores weight.")]
        private bool interactMode = false;

        [Header("Interact Mode Settings")]
        [SerializeField, Tooltip("Key the player presses to toggle when Interact Mode is on.")]
        private KeyCode interactKey = KeyCode.F;

        [Header("Weight Mode Settings")]
        [SerializeField, Tooltip("Total weight of objects on the platform required to activate. Objects publish their weight via WeightRequestEvent.")]
        private float weightThreshold;
        [SerializeField, Tooltip("If ON: platform deactivates when weight drops below threshold. If OFF: once activated, stays activated.")]
        private bool canDeactivate = true;

        [Header("Initial State")]
        [SerializeField, Tooltip("Start the platform already activated at level load (togglables enabled from frame 1).")]
        private bool startActivated = false;

        [Header("References")]
        [SerializeField, Tooltip("Objects that get enabled/disabled by this trigger (doors, platforms, hazards, etc.). Drag any Togglable component here.")]
        private List<Togglable> togglables = new List<Togglable>();
        
        private float _currentWeight;
        private bool _active;
        private bool _playerInZone;

        private HashSet<GameObject> AddedWeights { get; } = new();

        public bool IsActive => _active;

        public event Action OnActivated;
        public event Action OnDeactivated;

        private void Start()
        {
            if (startActivated)
            {
                _active = true;
                ActivateTogglables();
                OnActivated?.Invoke();
            }
            else
            {
                _active = false;
                DeactivateTogglables();
                OnDeactivated?.Invoke();
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
                if (other.GetComponent<PlayerMain>() != null)
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
                if (other.GetComponent<PlayerMain>() != null)
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

            ActivateTogglables();
            _active = true;
            OnActivated?.Invoke();
        }

        private void RemoveWeight(float value)
        {
            _currentWeight -= value;
            if (!(_currentWeight < weightThreshold) || !_active || !canDeactivate) return;

            DeactivateTogglables();
            _active = false;
            OnDeactivated?.Invoke();
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
                DeactivateTogglables();
                _active = false;
                OnDeactivated?.Invoke();
            }
            else
            {
                ActivateTogglables();
                _active = true;
                OnActivated?.Invoke();
            }
        }
    }
}