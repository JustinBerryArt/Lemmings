using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lemmings
{
    /// <summary>
    /// Runtime-capable visual debug tool for proxies.
    /// Shows position, radius, axis direction, and links to member objects.
    /// Color-codes status, supports demo visuals in builds.
    /// </summary>
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    [ExecuteAlways]
    public class LemmingProxyVisualizer : MonoBehaviour
    {
        [Header("Debug Toggle")]
        [Tooltip("Enable or disable runtime debug visuals.")]
        public bool useVisuals = true;

        [Header("Debug Labels")]
        public bool showLabel = true;
        
        [Header("Sphere Settings")]
        [Tooltip("Draw a wire sphere at the proxy's position.")]
        public bool drawSphere = true;
        public float radiusScale = 1f;

        [Header("Status Colors")]
        public Color inRangeColor = Color.green;
        public Color overColor = Color.red;
        public Color underColor = Color.yellow;

        [Header("Axis Settings")]
        [Tooltip("Draw transform axes (forward, right, up) at this position.")]
        public bool showAxes = true;
        public float axisLength = 0.2f;
        public float gazeLength =  0.2f;

        [Header("Link Lines")]
        [Tooltip("Draw lines to each member in the relationship.")]
        public bool showLinesToMembers = true;
        public Color lineColor = Color.cyan;

        private LemmingRelationshipProxy proxy;

        private void OnDrawGizmos()
        {
            if (!useVisuals)
                return;

            // Ensure reference in edit mode
            if (proxy == null)
                proxy = GetComponent<LemmingRelationshipProxy>();

            if (proxy == null || proxy.relationship == null)
                return;

            // Set gaze length dynamically if missing
            gazeLength = Mathf.Max(gazeLength, proxy.gazeRange);

            var rel = proxy.relationship;

            // -------- Color Selection by Status --------

            proxy.transform.position = RelationshipPreviewUtility.PreviewPosition(rel);
            proxy.transform.rotation = RelationshipPreviewUtility.PreviewRotation(rel);
            
            
            if (proxy.Data.IsTriggered)
            {
                Gizmos.color = Color.gray;
            } else             switch (rel.Status)
            {
                case RelationshipStatus.InRange: Gizmos.color = inRangeColor; break;
                case RelationshipStatus.Over:    Gizmos.color = overColor;    break;
                case RelationshipStatus.Under:   Gizmos.color = underColor;   break;
                default:                         Gizmos.color = Color.white;   break;
            }
            
            Vector3 pos = transform.position;
            float radius = proxy.ComputeRadius() * radiusScale;

            // -------- Sphere Visualization --------
            if (drawSphere)
            {
                Gizmos.DrawWireSphere(pos, radius);
            }

            // -------- Axis Arrows (Forward, Right, Up) --------
            if (showAxes)
            {
                Vector3 fwd = transform.forward * gazeLength;
                Vector3 up  = transform.up * axisLength;
                Vector3 rt  = transform.right * axisLength;

                if (proxy.Data.IsTriggered)
                {
                    Gizmos.color = Color.purple; Gizmos.DrawRay(pos, fwd); // gazing = purple
                }
                else
                {
                    Gizmos.color = Color.blue; Gizmos.DrawRay(pos, fwd);  // forward = blue
                }
 
                Gizmos.color = Color.green; Gizmos.DrawRay(pos, up);   // up = green
                Gizmos.color = Color.red; Gizmos.DrawRay(pos, rt);     // right = red
            }

            // -------- Relationship Lines --------
            if (showLinesToMembers)
            {
                Gizmos.color = lineColor;
                foreach (var member in rel.Members)
                {
                    if (member == null) continue;
                    Gizmos.DrawLine(pos, member.transform.position);
                }
            }
            
#if UNITY_EDITOR
            if (proxy != null && proxy.relationship != null && !string.IsNullOrEmpty(proxy.relationship.ID) && showLabel)
            {
                Vector3 labelPos = transform.position + Vector3.up * 0.1f;
                Handles.color = Color.white;
                Handles.Label(labelPos, $"⮞ {proxy.relationship.ID}");
            }
#endif
            
        }
    }
}
