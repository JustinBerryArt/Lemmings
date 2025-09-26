using UnityEngine;

namespace Lemmings
{
    /// <summary>
    /// Authoring component that marks a scene object for wireframe debug visual output.
    /// This component does not spawn child objects or LineRenderers; it only stores
    /// settings read by the RenderGraph pass at draw time.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LemmingVizTarget : MonoBehaviour
    {
        /// <summary>
        /// Selects how the radius is determined.
        /// </summary>
        public enum RadiusMode
        {
            /// <summary>Use <see cref="SphereCollider"/> radius on this GameObject if present.</summary>
            SphereCollider,
            /// <summary>Use the largest extent of the object's world-space renderer bounds.</summary>
            BoundsMaxExtent,
            /// <summary>Use a manually specified radius.</summary>
            Manual
        }


        [Header("Radius Source")]
        [Tooltip("How the circle radius is computed at render time.")]
        public RadiusMode radiusMode = RadiusMode.SphereCollider;

[Tooltip("Manual radius (meters) if Radius Mode = Manual.")]
public float manualRadius = 0.15f;


[Tooltip("Scale applied to detected radius (SphereCollider or Bounds).")]
public float radiusScale = 1.0f;


[Header("Appearance")]
[Tooltip("Base color (alpha controls line transparency). Suggested A≈0.7.")]
public Color lineColor = new Color(0.2f, 0.9f, 1.0f, 0.7f);


[Tooltip("Number of segments in the circle (>= 8). More segments = smoother circle.")]
[Min(8)] public int circleSegments = 48;


[Tooltip("Length (meters) of the orientation axes lines.")]
public float axisLength = 0.25f;


[Header("Toggles")]
[Tooltip("Draw the wire circle at the chosen radius.")]
public bool drawCircle = true;


[Tooltip("Draw vertical (+Y) and horizontal (+X and +Z) axes.")]
public bool drawAxes = true;


/// <summary>
/// Computes the radius for this target based on the selected <see cref="radiusMode"/>.
/// </summary>
/// <returns>Non-negative radius in meters.</returns>
public float ComputeRadius()
{
    float r = Mathf.Max(0.0001f, manualRadius);


    switch (radiusMode)
    {
        case RadiusMode.SphereCollider:
        {
            var sc = GetComponent<SphereCollider>();
            if (sc) r = Mathf.Abs(sc.radius) * Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
            break;
        }
        case RadiusMode.BoundsMaxExtent:
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
            {
                var b = new Bounds(transform.position, Vector3.zero);
                foreach (var rr in renderers) b.Encapsulate(rr.bounds);
                r = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z));
            }
            break;
        }
        case RadiusMode.Manual:
        default:
            break;
    }


    return Mathf.Max(0.0001f, r * Mathf.Max(0.0f, radiusScale));
}
}
}