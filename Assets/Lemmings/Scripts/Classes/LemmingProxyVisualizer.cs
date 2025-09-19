using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lemmings
{
    /// <summary>
    /// Runtime-capable visual debug tool for LemmingRelationshipProxy objects.
    /// Renders position spheres, axis lines, and member links in both Scene View and Game View.
    /// Also color-codes relationship status and displays labels if enabled.
    ///
    /// Intended for live debugging, user education, or demo playback in builds.
    /// </summary>
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    [ExecuteAlways]
    public class LemmingProxyVisualizer : MonoBehaviour
    {
        [FormerlySerializedAs("useVisuals")]
        [Header("Debug Toggle")]
        /// <summary>
        /// Master toggle to enable or disable all visual debug rendering.
        /// Affects both Scene View and runtime visuals.
        /// </summary>
        [Tooltip("Enable or disable runtime debug visuals.")]
        public bool useEditorVisuals = true;

        /// <summary>
        /// Toggles whether debug visuals should be drawn in the Game View at runtime (including builds).
        /// Uses GL lines and requires a material with an unlit shader (e.g. Unlit/Color).
        /// </summary>
        [Tooltip("Enable drawing of visuals in Game View (builds and play mode).")]
        public bool useRuntimeVisuals = true;

        [Header("Runtime References")]
        public GameObject spherePrefab;       // A simple wireframe or low-poly sphere
        public Material lineMaterial;         // Unlit/Color or Shader Graph-compatible
        public Transform axisContainer;

        private GameObject sphereInstance;
        private List<LineRenderer> axisLines = new();
        private List<LineRenderer> memberLines = new();
        
        /// <summary>
        /// The material used for runtime GL drawing.
        /// Must use a shader compatible with GL (such as Unlit/Color).
        /// </summary>
        [Tooltip("Assign a material with Shader: Unlit/Color")]
        public Material runtimeDebugMaterial;

        [Header("Debug Labels")]
        /// <summary>
        /// If enabled, draws a label with the relationship ID above the proxy.
        /// Only shown in Scene View during edit mode.
        /// </summary>
        public bool showLabel = true;

        [Header("Sphere Settings")]
        /// <summary>
        /// If true, draws a wireframe sphere representing the proxy's effective radius.
        /// </summary>
        [Tooltip("Draw a wire sphere at the proxy's position.")]
        public bool drawSphere = true;

        /// <summary>
        /// Multiplier applied to the computed radius when drawing the debug sphere.
        /// </summary>
        public float radiusScale = 1f;

        [Header("Status Colors")]
        /// <summary>Color used when the relationship is in the expected range.</summary>
        public Color inRangeColor = Color.green;

        /// <summary>Color used when the normalized value is above the max threshold.</summary>
        public Color overColor = Color.red;

        /// <summary>Color used when the normalized value is below the min threshold.</summary>
        public Color underColor = Color.yellow;

        [Header("Axis Settings")]
        /// <summary>
        /// If true, draws debug axes at the proxy's transform: forward (blue), up (green), right (red).
        /// </summary>
        [Tooltip("Draw transform axes (forward, right, up) at this position.")]
        public bool showAxes = true;

        /// <summary>Length of the up and right axes.</summary>
        public float axisLength = 0.2f;

        /// <summary>Length of the forward (or gaze) axis.</summary>
        public float gazeLength = 0.2f;

        [Header("Link Lines")]
        /// <summary>
        /// If true, draws a line from the proxy to each member GameObject in the relationship.
        /// </summary>
        [Tooltip("Draw lines to each member in the relationship.")]
        public bool showLinesToMembers = true;

        /// <summary>Color used for relationship link lines.</summary>
        public Color lineColor = Color.cyan;

        /// <summary>Cached reference to the attached LemmingRelationshipProxy component.</summary>
        private LemmingRelationshipProxy proxy;

        /// <summary>
        /// Safely initializes the <see cref="proxy"/> reference if it is null.
        /// </summary>
        private void EnsureProxy()
        {
            if (proxy == null)
                proxy = GetComponent<LemmingRelationshipProxy>();
        }

        /// <summary>
        /// Draws the relationship visualization in the Game View at runtime using GL.
        /// Only runs during Play Mode if <see cref="useRuntimeVisuals"/> is true.
        /// </summary>
        private void OnRenderObject()
        {
#if UNITY_EDITOR
            // Prevent double rendering in SceneView camera
            if (UnityEditor.SceneView.currentDrawingSceneView?.camera == Camera.current)
                return;
#endif
            if (!useEditorVisuals || !useRuntimeVisuals || !Application.isPlaying || runtimeDebugMaterial == null)
                return;

            runtimeDebugMaterial.SetPass(0);
            DrawProxyVisuals(useGizmos: false, useGL: true);
        }

        /// <summary>
        /// Draws the relationship visualization in Scene View using Gizmos.
        /// Called automatically by Unity Editor.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!useEditorVisuals || Application.isPlaying)
                return;

            DrawProxyVisuals(useGizmos: true, useGL: false);
        }

        /// <summary>
        /// Core method that renders all visual elements using Gizmos or GL depending on flags.
        /// Includes sphere, axes, lines to members, and label if applicable.
        /// </summary>
        /// <param name="useGizmos">Whether to use Gizmos for rendering (Scene View).</param>
        /// <param name="useGL">Whether to use GL for runtime rendering (Game View).</param>
        private void DrawProxyVisuals(bool useGizmos, bool useGL)
        {
            EnsureProxy();
            if (proxy == null || proxy.relationship == null)
                return;

            var rel = proxy.relationship;

            proxy.transform.position = RelationshipPreviewUtility.PreviewPosition(rel);
            proxy.transform.rotation = RelationshipPreviewUtility.PreviewRotation(rel);
            gazeLength = Mathf.Max(gazeLength, proxy.gazeRange);

            Vector3 pos = transform.position;
            float radius = proxy.ComputeRadius() * radiusScale;

            Color statusColor = proxy.Data.IsTriggered ? Color.gray : rel.Status switch
            {
                RelationshipStatus.InRange => inRangeColor,
                RelationshipStatus.Over => overColor,
                RelationshipStatus.Under => underColor,
                _ => Color.white
            };

            // ----- Sphere -----
            if (drawSphere)
            {
                if (useGizmos)
                {
                    Gizmos.color = statusColor;
                    Gizmos.DrawWireSphere(pos, radius);
                }
                if (useGL)
                    DrawWireSphere(pos, radius, statusColor);
            }

            // ----- Axes -----
            if (showAxes)
            {
                Vector3 fwd = transform.forward * gazeLength;
                Vector3 up = transform.up * axisLength;
                Vector3 rt = transform.right * axisLength;

                if (useGizmos)
                {
                    Gizmos.color = proxy.Data.IsTriggered ? Color.magenta : Color.blue;
                    Gizmos.DrawRay(pos, fwd);
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(pos, up);
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(pos, rt);
                }
                if (useGL)
                {
                    DrawLine(pos, pos + fwd, proxy.Data.IsTriggered ? Color.magenta : Color.blue);
                    DrawLine(pos, pos + up, Color.green);
                    DrawLine(pos, pos + rt, Color.red);
                }
            }

            // ----- Member Lines -----
            if (showLinesToMembers)
            {
                if (useGizmos)
                {
                    Gizmos.color = lineColor;
                    foreach (var member in rel.Members)
                    {
                        if (member != null)
                            Gizmos.DrawLine(pos, member.transform.position);
                    }
                }
                if (useGL)
                {
                    foreach (var member in rel.Members)
                    {
                        if (member != null)
                            DrawLine(pos, member.transform.position, lineColor);
                    }
                }
            }

#if UNITY_EDITOR
            if (useGizmos && showLabel && !string.IsNullOrEmpty(rel.ID))
            {
                Vector3 labelPos = pos + Vector3.up * 0.1f;
                Handles.color = Color.white;
                Handles.Label(labelPos, $"⮞ {rel.ID}");
            }
#endif
        }

        /// <summary>
        /// Called by URP GL render pass to manually draw visuals.
        /// </summary>
        public void RenderRuntimeVisualsGL()
        {
            if (!Application.isPlaying || !useRuntimeVisuals) return;
            DrawProxyVisuals(useGizmos: false, useGL: true);
        }
        
        
        /// <summary>
        /// Draws a 3D line in GL using the specified color.
        /// </summary>
        private void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            GL.Begin(GL.LINES);
            GL.Color(color);
            GL.Vertex(start);
            GL.Vertex(end);
            GL.End();
        }

        /// <summary>
        /// Draws a wireframe sphere using GL in three axes (XY, YZ, XZ).
        /// </summary>
        /// <param name="center">Center of the sphere.</param>
        /// <param name="radius">Radius of the sphere.</param>
        /// <param name="color">Color to use for drawing.</param>
        /// <param name="segments">Number of circle segments per axis.</param>
        private void DrawWireSphere(Vector3 center, float radius, Color color, int segments = 24)
        {
            GL.Begin(GL.LINES);
            GL.Color(color);
            for (int axis = 0; axis < 3; axis++)
            {
                for (int i = 0; i < segments; i++)
                {
                    float angle0 = i * Mathf.PI * 2 / segments;
                    float angle1 = (i + 1) * Mathf.PI * 2 / segments;

                    Vector3 p0 = GetCirclePoint(center, radius, angle0, axis);
                    Vector3 p1 = GetCirclePoint(center, radius, angle1, axis);

                    GL.Vertex(p0);
                    GL.Vertex(p1);
                }
            }
            GL.End();
        }

        /// <summary>
        /// Returns a point on a circle around the specified axis.
        /// </summary>
        /// <param name="center">Center of the circle.</param>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="angle">Angle in radians.</param>
        /// <param name="axis">0=X, 1=Y, 2=Z</param>
        private Vector3 GetCirclePoint(Vector3 center, float radius, float angle, int axis)
        {
            return axis switch
            {
                0 => center + new Vector3(0, Mathf.Cos(angle), Mathf.Sin(angle)) * radius,
                1 => center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius,
                2 => center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius,
                _ => center
            };
        }
    }
}