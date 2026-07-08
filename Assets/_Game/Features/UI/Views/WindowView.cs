using Light_and_controller.Scripts.Events;
using Light_and_controller.Scripts.Systems;
using UnityEngine;

namespace Light_and_controller.Scripts.UI
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