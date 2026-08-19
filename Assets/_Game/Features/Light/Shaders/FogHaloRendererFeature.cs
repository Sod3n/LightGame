using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LightGame.Features
{
    // FullScreenPassRendererFeature.AddRenderPasses runs once per camera in a
    // stack - base and every overlay - with no built-in filtering. Confirmed via
    // Frame Debugger in Play mode that this fired redundantly on the 3 UI-only
    // overlay cameras (PlayerUI/Windows/MainMenuUI, cullingMask=UI, no world
    // geometry) in addition to the Main Camera, which is why the halo worked in
    // isolated single-camera Editor screenshots but not in real Play mode.
    // Gating to the Base camera is the standard fix for a full-screen pass used
    // alongside camera stacking.
    public class FogHaloRendererFeature : FullScreenPassRendererFeature
    {
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.renderType != CameraRenderType.Base)
                return;

            base.AddRenderPasses(renderer, ref renderingData);
        }
    }
}
