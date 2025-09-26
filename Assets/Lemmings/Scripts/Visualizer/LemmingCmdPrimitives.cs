using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lemmings
{
    /// <summary>
    /// Creates and caches the minimal meshes and material required for drawing wire visuals.
    /// </summary>
    public static class LemmingCmdPrimitives
    {
        /// <summary>A unit circle on the XZ plane centered at origin, line-strip topology.</summary>
        public static Mesh UnitCircleXZ { get; private set; }
        /// <summary>A 2-vertex unit line along +X used as a template for axis and segment drawing.</summary>
        public static Mesh UnitLine { get; private set; }
        /// <summary>URP Unlit material used for all lines/circles. Color is set via _BaseColor.</summary>
        public static Material UnlitColor { get; private set; }

        private static int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static bool _initialized;

        /// <summary>
        /// Ensures meshes and materials are created exactly once.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            // Unit line (0..1 along +X)
            UnitLine = new Mesh { name = "Lemming.UnitLine" };
            UnitLine.SetVertices(new List<Vector3> { Vector3.zero, Vector3.right });
            UnitLine.SetIndices(new[] { 0, 1 }, MeshTopology.Lines, 0, false);
            UnitLine.UploadMeshData(true);

            // Unit circle (XZ plane). Default resolution 64; we will reuse indices for higher/lower
            // resolutions by drawing only the first N vertices via DrawProcedural if desired, but since
            // we use DrawMesh, we regenerate a smaller mesh per call in the helper (no allocations here).
            UnitCircleXZ = BuildCircleMesh(64);

            // URP Unlit material (transparent). If a custom shader is unavailable, fallback to URP/Unlit
            UnlitColor = CoreUtils.CreateEngineMaterial("Universal Render Pipeline/Unlit");
            UnlitColor.enableInstancing = true;
            // Transparent queue so alpha works; depth test on, depth write off — wireframe feel with occlusion
            UnlitColor.renderQueue = (int)RenderQueue.Transparent;
            UnlitColor.SetFloat("_ZWrite", 0f);
        }

        /// <summary>Releases meshes and material.</summary>
        public static void Cleanup()
        {
            if (Application.isPlaying)
            {
                if (UnitLine) Object.Destroy(UnitLine);
                if (UnitCircleXZ) Object.Destroy(UnitCircleXZ);
                if (UnlitColor) Object.Destroy(UnlitColor);
            }
            else
            {
                if (UnitLine) Object.DestroyImmediate(UnitLine);
                if (UnitCircleXZ) Object.DestroyImmediate(UnitCircleXZ);
                if (UnlitColor) Object.DestroyImmediate(UnlitColor);
            }
            UnitLine = null; UnitCircleXZ = null; UnlitColor = null; _initialized = false;
        }

        /// <summary>Returns the shader property id for _BaseColor on URP Unlit.</summary>
        public static int BaseColorId => _baseColorId;

        /// <summary>
        /// Builds a unit-radius circle mesh in the XZ plane with <paramref name="segments"/> vertices.
        /// </summary>
        private static Mesh BuildCircleMesh(int segments)
        {
            segments = Mathf.Max(8, segments);
            var m = new Mesh { name = $"Lemming.UnitCircleXZ.{segments}" };

            var verts = new Vector3[segments + 1];
            var indices = new int[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float t = (i % segments) / (float)segments;
                float a = t * Mathf.PI * 2f;
                verts[i] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                indices[i] = i;
            }

            m.SetVertices(verts);
            m.SetIndices(indices, MeshTopology.LineStrip, 0, false);
            m.UploadMeshData(true);
            return m;
        }

        /// <summary>
        /// Ensures a circle mesh exists with at least the requested segment count.
        /// For simplicity, this returns the default 64-segment circle for all requests ≥ 48.
        /// Callers that require exact segment counts may draw via <see cref="LemmingCmdHelpers.DrawCircle"/>
        /// which procedurally regenerates a temporary index buffer on the command buffer.
        /// </summary>
        public static Mesh GetCircleMesh(int segments)
        {
            if (segments >= 48) return UnitCircleXZ; // reuse default cache
            // Build a smaller mesh on demand (editor-only allocation). For play-mode stability, reuse default.
#if UNITY_EDITOR
            return BuildCircleMesh(Mathf.Clamp(segments, 8, 47));
#else
            return UnitCircleXZ;
#endif
        }
    }
}
