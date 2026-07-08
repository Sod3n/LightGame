using UnityEngine.EventSystems;

namespace Light_and_controller.Scripts
{
    public interface IInitializable : IEventSystemHandler
    {
        public void Initialize();
    }
}