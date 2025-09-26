using UnityEngine;
using UnityEngine.Rendering;

namespace Lemmings
{
    /// <summary>
    /// Small, allocation-free helpers for issuing line and circle draws with a <see cref="CommandBuffer"/>.
    /// </summary>
    public static class LemmingCmdHelpers
    {
        /// <summary>
        /// Sets the global color for the shared Unlit material (_BaseColor). Use before each draw call
        /// when per-primitive color is needed without creating material instances.
        /// </summary>
        public static void SetColor(CommandBuffer cmd, Color color)
        {
            cmd.SetGlobalColor(LemmingCmdPrimitives.BaseColorId, color);
        }

        /// <summary>
        /// Draws a line from <paramref name="a"/> to <paramref name="b"/>.
        /// This uses a 2-vertex mesh in <see cref="MeshTopology.Lines"/> scaled along +X.
        /// </summary>
        public static void DrawLine(CommandBuffer cmd, Vector3 a, Vector3 b, Color color)
        {
            var dir = b - a; var len = dir.magnitude;
            if (len <= 1e-6f) return;
            var rot = Quaternion.FromToRotation(Vector3.right, dir / len);
            var trs = Matrix4x4.TRS(a, rot, new Vector3(len, 1f, 1f));
            SetColor(cmd, color);
            cmd.DrawMesh(LemmingCmdPrimitives.UnitLine, trs, LemmingCmdPrimitives.UnlitColor, 0, 0);
        }

        /// <summary>
        /// Draws a circle wireframe in the XZ plane of the provided <paramref name="rotation"/>, scaled by
        /// <paramref name="radius"/> and centered at <paramref name="center"/>.
        /// </summary>
        /// <param name="segments">Number of segments (>=8). Higher = smoother circle.</param>
        public static void DrawCircle(CommandBuffer cmd, Vector3 center, Quaternion rotation, float radius, int segments, Color color)
        {
            segments = Mathf.Max(8, segments);
            var mesh = LemmingCmdPrimitives.GetCircleMesh(segments);
            var trs = Matrix4x4.TRS(center, rotation, Vector3.one * radius);
            SetColor(cmd, color);
            cmd.DrawMesh(mesh, trs, LemmingCmdPrimitives.UnlitColor, 0, 0);
        }
        
        // BELOW --> These are overload methods with RasterCommandBuffer rather than CommandBuffer
        
        public static void SetColor(RasterCommandBuffer cmd, Color color)
        {
            cmd.SetGlobalColor(LemmingCmdPrimitives.BaseColorId, color);
        }

        public static void DrawLine(RasterCommandBuffer cmd, Vector3 a, Vector3 b, Color color)
        {
            var dir = b - a; var len = dir.magnitude;
            if (len <= 1e-6f) return;
            var rot = Quaternion.FromToRotation(Vector3.right, dir / len);
            var trs = Matrix4x4.TRS(a, rot, new Vector3(len, 1f, 1f));
            SetColor(cmd, color);
            cmd.DrawMesh(LemmingCmdPrimitives.UnitLine, trs, LemmingCmdPrimitives.UnlitColor, 0, 0);
        }

        public static void DrawCircle(RasterCommandBuffer cmd, Vector3 center, Quaternion rotation,
            float radius, int segments, Color color)
        {
            segments = Mathf.Max(8, segments);
            var mesh = LemmingCmdPrimitives.GetCircleMesh(segments);
            var trs  = Matrix4x4.TRS(center, rotation, Vector3.one * radius);
            SetColor(cmd, color);
            cmd.DrawMesh(mesh, trs, LemmingCmdPrimitives.UnlitColor, 0, 0);
        }
        
        
    }
}