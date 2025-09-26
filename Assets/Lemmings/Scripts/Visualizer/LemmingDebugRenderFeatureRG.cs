using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Lemmings
{
    /// <summary>
    /// Universal Render Pipeline RendererFeature that draws the consolidated wireframe
    /// visuals for all active <see cref="LemmingVizTarget"/> components via RenderGraph.
    /// It also supports URP Compatibility Mode by implementing <see cref="ScriptableRenderPass.Execute"/>.
    /// </summary>
    public sealed class LemmingDebugRenderFeatureRG : ScriptableRendererFeature
    {
        /// <summary>
        /// The ScriptableRenderPass that records rendering work into the RenderGraph
        /// (or, when RG is disabled, draws directly via <see cref="CommandBuffer"/>).
        /// </summary>
        private sealed class LemmingDebugRenderPassRG : ScriptableRenderPass
        {
            private const string kProfiler = "Lemming Wireframe Debug (RG)";
            private readonly List<LemmingVizTarget> _targets = new();

            /// <summary>
            /// Initializes the pass to run after the main scene rendering so the visuals appear on top.
            /// </summary>
            public LemmingDebugRenderPassRG()
            {
                renderPassEvent = RenderPassEvent.AfterRendering;
            }

            /// <summary>
            /// Finds all active <see cref="LemmingVizTarget"/> components in the scene each frame.
            /// This keeps the system responsive to add/remove without manual registration.
            /// </summary>
            private void GatherTargets()
            {
                _targets.Clear();
                _targets.AddRange(Object.FindObjectsByType<LemmingVizTarget>(FindObjectsSortMode.None));
            }

            /// <summary>
            /// Per-pass payload captured by the RenderGraph.
            /// </summary>
            private sealed class PassData
            {
                public TextureHandle color;
                public TextureHandle depth;
                public Camera camera;
                public List<LemmingVizTarget> targets;
            }

            /// <inheritdoc/>
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                GatherTargets();
                if (_targets.Count == 0) return;

                var res = frameData.Get<UniversalResourceData>();
                var cam = frameData.Get<UniversalCameraData>().camera;

                using var builder = renderGraph.AddRasterRenderPass<PassData>(kProfiler, out var passData);
                passData.color   = res.activeColorTexture;
                passData.depth   = res.activeDepthTexture;
                passData.camera  = cam;
                passData.targets = _targets;

                builder.SetRenderAttachment(passData.color, 0);
                builder.SetRenderAttachmentDepth(passData.depth, AccessFlags.Read);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc<PassData>((PassData data, RasterGraphContext ctx) =>
                {
                    using (new ProfilingScope(ctx.cmd, new ProfilingSampler(kProfiler)))
                        DrawAll(ctx.cmd, data.camera, data.targets); // see C) for overload
                });
            }

            /// <inheritdoc/>
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                GatherTargets();
                if (_targets.Count == 0) return;

                var cmd = CommandBufferPool.Get(kProfiler);
                DrawAll(cmd, renderingData.cameraData.camera, _targets);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            /// <summary>
            /// Issues all draw commands for the supplied targets.
            /// </summary>
            private static void DrawAll(CommandBuffer cmd, Camera cam, List<LemmingVizTarget> targets)
            {
                if (!cam) return;
                LemmingCmdPrimitives.EnsureInitialized();

                foreach (var t in targets)
                {
                    if (!t || !t.isActiveAndEnabled) continue;

                    var radius = t.drawCircle ? t.ComputeRadius() : 0f;
                    var pos    = t.transform.position;
                    var rot    = t.transform.rotation;

                    // Circle (wireframe) -----------------------------------------------------
                    if (t.drawCircle && radius > 0f)
                    {
                        LemmingCmdHelpers.DrawCircle(cmd, pos, rot, radius, t.circleSegments, t.lineColor);
                    }

                    // Axes: vertical (+Y) and horizontal (+X and +Z) -------------------------
                    if (t.drawAxes && t.axisLength > 0f)
                    {
                        var basis = Matrix4x4.Rotate(rot);
                        var up = basis.MultiplyVector(Vector3.up);
                        var right = basis.MultiplyVector(Vector3.right);
                        var fwd = basis.MultiplyVector(Vector3.forward);
                        var L = t.axisLength;

                        // Vertical (+Y)
                        LemmingCmdHelpers.DrawLine(cmd, pos, pos + up * L, new Color(0.9f, 0.9f, 0.9f, t.lineColor.a));
                        // Horizontal (+X)
                        LemmingCmdHelpers.DrawLine(cmd, pos, pos + right * L, new Color(1.0f, 0.25f, 0.25f, t.lineColor.a));
                        // Horizontal (+Z)
                        LemmingCmdHelpers.DrawLine(cmd, pos, pos + fwd * L, new Color(0.25f, 0.55f, 1.0f, t.lineColor.a));
                    }
                }
            }
            
            /// <summary>
            /// Overload method with RasterCommandBuffer instead of CommandBuffer
            /// </summary>
            private static void DrawAll(RasterCommandBuffer cmd, Camera cam, List<LemmingVizTarget> targets)
            {
                if (!cam) return;
                LemmingCmdPrimitives.EnsureInitialized();

                foreach (var t in targets)
                {
                    if (!t || !t.isActiveAndEnabled) continue;

                    var radius = t.drawCircle ? t.ComputeRadius() : 0f;
                    var pos    = t.transform.position;
                    var rot    = t.transform.rotation;

                    if (t.drawCircle && radius > 0f)
                        LemmingCmdHelpers.DrawCircle(cmd, pos, rot, radius, t.circleSegments, t.lineColor);

                    if (t.drawAxes && t.axisLength > 0f)
                    {
                        var m = Matrix4x4.Rotate(rot);
                        var up = m.MultiplyVector(Vector3.up);
                        var right = m.MultiplyVector(Vector3.right);
                        var fwd = m.MultiplyVector(Vector3.forward);
                        var L = t.axisLength;

                        LemmingCmdHelpers.DrawLine(cmd, pos, pos + up   * L, new Color(0.9f, 0.9f, 0.9f, t.lineColor.a));
                        LemmingCmdHelpers.DrawLine(cmd, pos, pos + right* L, new Color(1.0f, 0.25f, 0.25f, t.lineColor.a));
                        LemmingCmdHelpers.DrawLine(cmd, pos, pos + fwd  * L, new Color(0.25f, 0.55f, 1.0f, t.lineColor.a));
                    }
                }
            }
            
        }

        private LemmingDebugRenderPassRG _pass;

        /// <inheritdoc/>
        public override void Create()
        {
            _pass = new LemmingDebugRenderPassRG();
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_pass);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                LemmingCmdPrimitives.Cleanup();
        }
    }
}