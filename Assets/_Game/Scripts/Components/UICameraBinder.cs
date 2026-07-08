using System.Collections.Generic;
using UnityEngine;

namespace Light_and_controller.Scripts.Components
{
    public class UICameraBinder : MonoBehaviour
    {
        private static readonly Dictionary<string, Camera> Cameras = new Dictionary<string, Camera>();
        
        public enum CameraKeyType
        {
            MainMenuUI,
            Windows,
            PlayerUI
        }
        
        [SerializeField] private CameraKeyType cameraKey = CameraKeyType.MainMenuUI;
        [SerializeField] private Canvas canvas;
        
        private bool isProvider;
        
        private void OnValidate()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
        }
        
        private void Awake()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            
            // If this object has a camera, register it as a provider
            if (TryGetComponent<Camera>(out var cam))
            {
                isProvider = true;
                Cameras[cameraKey.KeyToString()] = cam;
            }
        }
        
        private void OnDestroy()
        {
            if (isProvider)
            {
                Cameras.Remove(cameraKey.KeyToString());
            }
        }
        
        private void Update()
        {
            if (canvas && !isProvider)
            {
                if (Cameras.TryGetValue(cameraKey.KeyToString(), out var cam))
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                }
            }
        }
        
        public void SetCameraKey(CameraKeyType key)
        {
            if (isProvider)
            {
                Cameras.Remove(cameraKey.KeyToString());
                cameraKey = key;
                if (TryGetComponent<Camera>(out var cam))
                {
                    Cameras[cameraKey.KeyToString()] = cam;
                }
            }
            else
            {
                cameraKey = key;
            }
        }
    }
}