using System;
using System.Collections.Generic;

namespace LightGame.Globals
{
    /// <summary>
    /// Screen-stack façade. Mirrors Godot's <c>Screens</c> autoload
    /// (globals/screens.gd) — provides a static access point for
    /// push/pop/change semantics without holding a hard reference to
    /// the concrete screen container.
    ///
    /// The stack itself is bookkeeping; subscribers listen to
    /// <see cref="Changed"/> to render the top screen. UI code that
    /// wants to react to navigation should subscribe once at bootstrap.
    /// </summary>
    public static class Screens
    {
        private static readonly Stack<object> _stack = new();

        /// <summary>Fires whenever the top-of-stack changes (push, pop, change, popAll).</summary>
        public static event Action<object> Changed;

        public static object Current => _stack.Count > 0 ? _stack.Peek() : null;
        public static int Depth => _stack.Count;

        /// <summary>Replace the entire stack with a single screen.</summary>
        public static void Change(object screenKey)
        {
            _stack.Clear();
            _stack.Push(screenKey);
            Emit();
        }

        public static void Push(object screenKey)
        {
            _stack.Push(screenKey);
            Emit();
        }

        public static void Pop()
        {
            if (_stack.Count == 0) return;
            _stack.Pop();
            Emit();
        }

        public static void PopAll()
        {
            if (_stack.Count == 0) return;
            _stack.Clear();
            Emit();
        }

        private static void Emit() => Changed?.Invoke(Current);
    }
}
