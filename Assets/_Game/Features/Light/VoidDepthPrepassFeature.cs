using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace LightGame.Rendering
{
    /// <summary>
    /// Renders every gameplay sprite's WORLD-space Z (remapped to 0..1) into a global
    /// single-channel texture <c>_VoidDepthTexture</c>, so VoidLightBuffer.shader can keep
    /// object depth readable while the scene is painted void-dark. World Z is inherently
    /// global: a child sprite's world Z already folds in every parent transform, so setting
    /// Z on a root (or a per-root SortingGroup->Z sync) propagates depth to all its children
    /// for free. Sorting is untouched - this is a parallel depth signal, not a sort change.
    ///
    /// Pixels with no gameplay object read back -1 (the clear sentinel), letting the void
    /// shader fall back to a neutral fill there. The fullscreen void/fog quads are excluded
    /// by render-queue range (they live in Overlay/4000; sprites are Transparent/3000), and
    /// the pass runs BEFORE transparents so the texture is ready when the void quad samples it.
    /// </summary>
    public class VoidDepthPrepassFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Which GameObject layers contribute depth. Fullscreen void/fog quads are already excluded by render queue, so Everything is usually fine.")]
            public LayerMask layerMask = ~0;

            [Tooltip("BeforeRenderingTransparents: depth is written before the void quad (Overlay/4000) draws, so it's same-frame and tracks the camera with no lag.")]
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;

            [Tooltip("World Z that maps to depth 0 (rendered nearest / front).")]
            public float worldZNear = -5f;

            [Tooltip("World Z that maps to depth 1 (rendered farthest / back).")]
            public float worldZFar = 5f;

            [Tooltip("Upper render-queue bound; keep below 4000 to exclude the Overlay-queue void/fog quads.")]
            public int maxRenderQueue = 3500;
        }

        public Settings settings = new Settings();

        private Material _material;
        private VoidDepthPass _pass;

        public override void Create()
        {
            var shader = Shader.Find("Hidden/LightGame/VoidDepth");
            if (shader != null)
                _material = CoreUtils.CreateEngineMaterial(shader);

            _pass = new VoidDepthPass(_material, settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_material == null || _pass == null)
                return;

            // Run ONLY on the main gameplay camera. The UI cameras render after it and share
            // the global _VoidDepthTexture; letting them run would clear it to the empty
            // sentinel (they cull only UI), leaving the void quad reading nothing.
            var cam = renderingData.cameraData.camera;
            if (cam == null || cam.cameraType != CameraType.Game || !cam.CompareTag("MainCamera"))
                return;

            _pass.renderPassEvent = settings.renderPassEvent;
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
            _pass?.Dispose();
        }

        private class VoidDepthPass : ScriptableRenderPass
        {
            private readonly Material _material;
            private readonly Settings _settings;

            private static readonly List<ShaderTagId> Tags = new List<ShaderTagId>
            {
                new ShaderTagId("Universal2D"),
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("UniversalForward"),
            };

            private static readonly int NearId = Shader.PropertyToID("_DepthNear");
            private static readonly int FarId = Shader.PropertyToID("_DepthFar");
            private static readonly int VoidDepthTexId = Shader.PropertyToID("_VoidDepthTexture");

            // Persistent (imported) target so RenderGraph can't alias/free its memory before
            // URP's transparent pass (which draws the void quad) samples the global.
            private RTHandle _depthHandle;

            public VoidDepthPass(Material material, Settings settings)
            {
                _material = material;
                _settings = settings;
            }

            public void Dispose()
            {
                _depthHandle?.Release();
                _depthHandle = null;
            }

            private class PassData
            {
                public RendererListHandle RendererList;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (_material == null)
                    return;

                _material.SetFloat(NearId, _settings.worldZNear);
                _material.SetFloat(FarId, _settings.worldZFar);

                var renderingData = frameData.Get<UniversalRenderingData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                var lightData = frameData.Get<UniversalLightData>();

                // Single-channel float target so the -1 "no object" sentinel survives.
                var desc = cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;
                desc.graphicsFormat = GraphicsFormat.R16_SFloat;
                desc.useMipMap = false;
                desc.autoGenerateMips = false;

                RenderingUtils.ReAllocateHandleIfNeeded(
                    ref _depthHandle, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_VoidDepthTexture");

                // Bind as a global directly against the persistent RT. SetGlobalTextureAfterPass
                // did not propagate to URP 2D's built-in transparent pass (which draws the void
                // quad); this CPU-side global on a stable handle is honored by every later draw.
                Shader.SetGlobalTexture(VoidDepthTexId, _depthHandle);

                TextureHandle depthTex = renderGraph.ImportTexture(_depthHandle);

                var drawSettings = RenderingUtils.CreateDrawingSettings(
                    Tags, renderingData, cameraData, lightData, SortingCriteria.CommonTransparent);
                drawSettings.overrideMaterial = _material;
                drawSettings.overrideMaterialPassIndex = 0;

                var queue = new RenderQueueRange(0, Mathf.Clamp(_settings.maxRenderQueue, 1, 5000));
                var filterSettings = new FilteringSettings(queue, _settings.layerMask);
                var rlParams = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("VoidDepthPrepass", out var passData))
                {
                    passData.RendererList = renderGraph.CreateRendererList(rlParams);
                    builder.UseRendererList(passData.RendererList);
                    builder.SetRenderAttachment(depthTex, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    {
                        // -1 everywhere = "no object"; drawn sprites overwrite with 0..1 world Z.
                        ctx.cmd.ClearRenderTarget(RTClearFlags.Color, new Color(-1f, -1f, -1f, -1f), 1f, 0);
                        ctx.cmd.DrawRendererList(data.RendererList);
                    });
                }
            }
        }
    }
}
