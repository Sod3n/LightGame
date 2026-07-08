using System;
using UnityEngine;

namespace LightGame.Core
{
    /// <summary>
    /// Base class for data models. Mirrors Godot's <c>class_name Model extends Resource</c>.
    ///
    /// Concrete models expose fields as properties with equality-guarded setters
    /// that raise <see cref="PropertyChanged"/>. Views subscribe and re-render on change.
    /// </summary>
    public abstract class Model : ScriptableObject
    {
        public event Action<string, object> PropertyChanged;

        protected bool Set<T>(ref T backing, T value, string propertyName)
        {
            if (backing == null ? value == null : backing.Equals(value)) return false;
            backing = value;
            PropertyChanged?.Invoke(propertyName, value);
            return true;
        }
    }
}
