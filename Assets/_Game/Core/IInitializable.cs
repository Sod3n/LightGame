using UnityEngine.EventSystems;

namespace LightGame.Core
{
    public interface IInitializable : IEventSystemHandler
    {
        public void Initialize();
    }
}