using LightGame.Events;
using LightGame.Globals;
using UnityEngine;

namespace LightGame.Features.UI
{
    public class WindowView<VT, Dt> : MonoBehaviour
    {
        public void Awake()
        {
            EventBus.Subscribe<OpenWindowEvent<VT, Dt>>(OnOpen);
            EventBus.Subscribe<CloseWindowEvent<VT>>(OnClose);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OpenWindowEvent<VT, Dt>>(OnOpen);
            EventBus.Unsubscribe<CloseWindowEvent<VT>>(OnClose);
        }
        
        protected virtual void OnOpen(OpenWindowEvent<VT, Dt> eventData)
        {
            gameObject.SetActive(true);
        }
        
        protected virtual void OnClose(CloseWindowEvent<VT> eventData)
        {
            gameObject.SetActive(false);
        }
    }
}