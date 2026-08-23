using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace Core.Client.UI.Components
{
    [RequireComponent(typeof(Button))]
    public class ButtonOpenURL : MonoBehaviour
    {
        [SerializeField] private LocalizedString url;

        private Button _button;
        
        
        private void OnEnable()
        {
            _button ??= GetComponent<Button>();
            _button.onClick.AddListener(OpenURL);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OpenURL);
        }

        private void OpenURL()
        {
            Application.OpenURL(url.GetLocalizedString());
        }
    }
}