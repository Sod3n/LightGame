using UnityEngine;

namespace Light_and_controller.Scripts
{
    /// <summary>
    /// Abstract base class for game elements that can be toggled on/off
    /// </summary>
    public abstract class Togglable : MonoBehaviour
    {
        /// <summary>
        /// Enable the togglable element
        /// </summary>
        public abstract void Enable();

        /// <summary>
        /// Disable the togglable element
        /// </summary>
        public abstract void Disable();

        /// <summary>
        /// Set the active state of the togglable element
        /// </summary>
        /// <param name="active">True to enable, false to disable</param>
        public virtual void SetActive(bool active)
        {
            if (active)
                Enable();
            else
                Disable();
        }
    }
}
