using UnityEngine;

namespace LightGame.Globals
{
    /// <summary>
    /// Player facade. Mirrors Godot's <c>Player</c> autoload
    /// (globals/player.gd). Holds a resolved reference to the current
    /// player GameObject so features can access it without repeated
    /// <c>FindGameObjectWithTag</c> calls.
    ///
    /// Populated lazily on first access; cleared on scene change if
    /// the referenced object is destroyed.
    /// </summary>
    public static class Player
    {
        private static GameObject _cached;

        public static GameObject GameObject
        {
            get
            {
                if (_cached == null)
                    _cached = UnityEngine.GameObject.FindGameObjectWithTag("Player");
                return _cached;
            }
        }

        public static Transform Transform => GameObject != null ? GameObject.transform : null;

        public static void Reset() => _cached = null;
    }
}
