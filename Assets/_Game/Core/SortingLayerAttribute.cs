using UnityEngine;

namespace LightGame.Core
{
    /// <summary>
    /// Field attribute for int fields that hold a SortingLayer ID.
    /// Editor tooling can render a sorting-layer dropdown; runtime behavior is a no-op.
    /// </summary>
    public class SortingLayerAttribute : PropertyAttribute
    {
    }
}
