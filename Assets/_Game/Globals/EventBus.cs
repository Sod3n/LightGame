using System;
using System.Collections.Generic;
using UnityEngine;

namespace Light_and_controller.Scripts.Systems
{
    /// <summary>
    /// Global event bus system for decoupled communication between components
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _eventSubscribers = new Dictionary<Type, List<Delegate>>();
        private static readonly Dictionary<GameObject, Dictionary<Type, List<Delegate>>> _gameObjectEventSubscribers = new Dictionary<GameObject, Dictionary<Type, List<Delegate>>>();

        /// <summary>
        /// Subscribe to an event of type T
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="handler">Event handler callback</param>
        public static void Subscribe<T>(Action<T> handler) where T : class
        {
            Type eventType = typeof(T);

            if (!_eventSubscribers.ContainsKey(eventType))
            {
                _eventSubscribers[eventType] = new List<Delegate>();
            }

            _eventSubscribers[eventType].Add(handler);
        }

        /// <summary>
        /// Subscribe to an event of type T on a specific GameObject
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="target">Target GameObject</param>
        /// <param name="handler">Event handler callback</param>
        public static void Subscribe<T>(GameObject target, Action<T> handler) where T : class
        {
            if (target == null)
            {
                UnityEngine.Debug.LogError("Cannot subscribe to event on null GameObject");
                return;
            }

            Type eventType = typeof(T);

            if (!_gameObjectEventSubscribers.ContainsKey(target))
            {
                _gameObjectEventSubscribers[target] = new Dictionary<Type, List<Delegate>>();
            }

            if (!_gameObjectEventSubscribers[target].ContainsKey(eventType))
            {
                _gameObjectEventSubscribers[target][eventType] = new List<Delegate>();
            }

            _gameObjectEventSubscribers[target][eventType].Add(handler);
        }

        /// <summary>
        /// Unsubscribe from an event of type T
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="handler">Event handler callback to remove</param>
        public static void Unsubscribe<T>(Action<T> handler) where T : class
        {
            Type eventType = typeof(T);

            if (_eventSubscribers.ContainsKey(eventType))
            {
                _eventSubscribers[eventType].Remove(handler);

                // Clean up empty lists
                if (_eventSubscribers[eventType].Count == 0)
                {
                    _eventSubscribers.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// Unsubscribe from an event of type T on a specific GameObject
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="target">Target GameObject</param>
        /// <param name="handler">Event handler callback to remove</param>
        public static void Unsubscribe<T>(GameObject target, Action<T> handler) where T : class
        {
            if (target == null || !_gameObjectEventSubscribers.ContainsKey(target))
            {
                return;
            }

            Type eventType = typeof(T);

            if (_gameObjectEventSubscribers[target].ContainsKey(eventType))
            {
                _gameObjectEventSubscribers[target][eventType].Remove(handler);

                // Clean up empty lists
                if (_gameObjectEventSubscribers[target][eventType].Count == 0)
                {
                    _gameObjectEventSubscribers[target].Remove(eventType);
                }

                // Clean up empty GameObject entries
                if (_gameObjectEventSubscribers[target].Count == 0)
                {
                    _gameObjectEventSubscribers.Remove(target);
                }
            }
        }

        /// <summary>
        /// Publish an event to all subscribers
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="eventData">Event data to publish</param>
        public static void Publish<T>(T eventData) where T : class
        {
            Type eventType = typeof(T);

            if (_eventSubscribers.ContainsKey(eventType))
            {
                // Create a copy to avoid issues if subscribers modify the list
                List<Delegate> subscribers = new List<Delegate>(_eventSubscribers[eventType]);

                foreach (Delegate subscriber in subscribers)
                {
                    try
                    {
                        (subscriber as Action<T>)?.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing other subscribers
                        UnityEngine.Debug.LogError($"Error invoking event handler for {eventType.Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Publish an event to all subscribers on a specific GameObject
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="target">Target GameObject</param>
        /// <param name="eventData">Event data to publish</param>
        public static void Publish<T>(GameObject target, T eventData) where T : class
        {
            if (target == null || !_gameObjectEventSubscribers.ContainsKey(target))
            {
                return;
            }

            Type eventType = typeof(T);

            if (_gameObjectEventSubscribers[target].ContainsKey(eventType))
            {
                // Create a copy to avoid issues if subscribers modify the list
                List<Delegate> subscribers = new List<Delegate>(_gameObjectEventSubscribers[target][eventType]);

                foreach (Delegate subscriber in subscribers)
                {
                    try
                    {
                        (subscriber as Action<T>)?.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing other subscribers
                        UnityEngine.Debug.LogError($"Error invoking GameObject event handler for {eventType.Name} on {target.name}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Clear all event subscriptions
        /// </summary>
        public static void ClearAll()
        {
            _eventSubscribers.Clear();
            _gameObjectEventSubscribers.Clear();
        }

        /// <summary>
        /// Clear all subscriptions for a specific event type
        /// </summary>
        /// <typeparam name="T">Event type to clear</typeparam>
        public static void Clear<T>() where T : class
        {
            Type eventType = typeof(T);
            if (_eventSubscribers.ContainsKey(eventType))
            {
                _eventSubscribers.Remove(eventType);
            }
        }

        /// <summary>
        /// Clear all subscriptions for a specific GameObject
        /// </summary>
        /// <param name="target">Target GameObject</param>
        public static void Clear(GameObject target)
        {
            if (target != null && _gameObjectEventSubscribers.ContainsKey(target))
            {
                _gameObjectEventSubscribers.Remove(target);
            }
        }

        /// <summary>
        /// Clear all subscriptions for a specific event type on a specific GameObject
        /// </summary>
        /// <typeparam name="T">Event type to clear</typeparam>
        /// <param name="target">Target GameObject</param>
        public static void Clear<T>(GameObject target) where T : class
        {
            if (target == null || !_gameObjectEventSubscribers.ContainsKey(target))
            {
                return;
            }

            Type eventType = typeof(T);
            if (_gameObjectEventSubscribers[target].ContainsKey(eventType))
            {
                _gameObjectEventSubscribers[target].Remove(eventType);

                // Clean up empty GameObject entries
                if (_gameObjectEventSubscribers[target].Count == 0)
                {
                    _gameObjectEventSubscribers.Remove(target);
                }
            }
        }

        /// <summary>
        /// Get the number of subscribers for a specific event type
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <returns>Number of subscribers</returns>
        public static int GetSubscriberCount<T>() where T : class
        {
            Type eventType = typeof(T);
            return _eventSubscribers.ContainsKey(eventType) ? _eventSubscribers[eventType].Count : 0;
        }

        /// <summary>
        /// Get the number of subscribers for a specific event type on a specific GameObject
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="target">Target GameObject</param>
        /// <returns>Number of subscribers</returns>
        public static int GetSubscriberCount<T>(GameObject target) where T : class
        {
            if (target == null || !_gameObjectEventSubscribers.ContainsKey(target))
            {
                return 0;
            }

            Type eventType = typeof(T);
            return _gameObjectEventSubscribers[target].ContainsKey(eventType) 
                ? _gameObjectEventSubscribers[target][eventType].Count 
                : 0;
        }
    }
}