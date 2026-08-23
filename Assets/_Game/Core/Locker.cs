using System;
using System.Collections.Generic;

namespace LightGame.Core
{
    /// <summary>
    /// Counting + named lock. Mirrors Godot's <c>features/core/locker.gd</c>.
    ///
    /// Used to gate state updates while animations play. Systems that mutate
    /// state check <see cref="IsLocked"/> and defer when non-zero. Callers
    /// hold locks either by count (transient) or by name (until an event).
    /// </summary>
    public sealed class Locker
    {
        private int _count;
        private readonly HashSet<string> _names = new HashSet<string>();

        public event Action<bool> Changed;

        public bool IsLocked => _count > 0 || _names.Count > 0;

        /// <summary>Take a counted lock. Invoke the returned Action to release.</summary>
        public Action Acquire()
        {
            _count++;
            EmitChanged();
            var released = false;
            return () =>
            {
                if (released) return;
                released = true;
                _count--;
                EmitChanged();
            };
        }

        public void Lock(string name)
        {
            if (_names.Add(name)) EmitChanged();
        }

        public void Unlock(string name)
        {
            if (_names.Remove(name)) EmitChanged();
        }

        private void EmitChanged() => Changed?.Invoke(IsLocked);
    }
}
