using System.Linq;
using UnityEngine;

namespace Lemmings
{
    /// <summary>
    /// Runtime visualizer for a LemmingRelationshipProxy.
    /// Spawns a prefab sphere sized to the proxy's SphereCollider,
    /// tints it by relationship status, and renders axis/gaze lines.
    /// </summary>
    [RequireComponent(typeof(LemmingRelationshipProxy))]
    [RequireComponent(typeof(SphereCollider))]
    public class LemmingProxyRuntimeVisualizer : MonoBehaviour
    {
        [Header("Prefab Sphere")]
        [Tooltip("A simple sphere prefab with a MeshRenderer. Its scale will be driven by the proxy collider.")]
        public GameObject spherePrefab;

        [Tooltip("Optional: Override the renderer material on the spawned sphere. If null, uses the prefab's material.")]
        public Material materialOverride;

        [Header("Colors by Status")]
        public Color colorInRange = new Color(0.2f, 1f, 0.2f, 0.35f);
        public Color colorOver    = new Color(1f, 0.2f, 0.2f, 0.35f);
        public Color colorUnder   = new Color(0.2f, 0.4f, 1f, 0.35f);
        public Color colorNone    = new Color(1f, 0.9f, 0.2f, 0.25f);

        [Header("Axis Lines")]
        public bool drawAxes = true;
        public float axisLengthMultiplier = 1.0f; // 1 = radius length, 2 = diameter, etc.
        public float lineWidth = 0.003f;
        public Color xColor = new Color(1f, 0.25f, 0.25f, 1f);
        public Color yColor = new Color(0.25f, 1f, 0.25f, 1f);
        public Color zColor = new Color(0.25f, 0.6f, 1f, 1f);

        [Header("Gaze Line")]
        public bool drawGaze = true;
        [Tooltip("Leave empty to use this proxy's forward. Otherwise specify a Transform (e.g., a head/hand).")]
        public Transform gazeSource;
        public float gazeLength = 1.0f;
        public Color gazeColor = new Color(0f, 1f, 1f, 1f);

        // refs
        LemmingRelationshipProxy proxy;
        SphereCollider sphereCol;
        Transform sphereT;
        MeshRenderer sphereMR;
        LineRenderer xLine, yLine, zLine, gazeLine;
        MaterialPropertyBlock mpb;

        void Awake()
        {
            proxy = GetComponent<LemmingRelationshipProxy>();
            sphereCol = GetComponent<SphereCollider>();
            mpb = new MaterialPropertyBlock();

            // Spawn sphere (child)
            if (spherePrefab != null)
            {
                var sphere = Instantiate(spherePrefab, transform);
                sphere.name = "ProxySphereVisual";
                sphereT = sphere.transform;
                sphereMR = sphere.GetComponentInChildren<MeshRenderer>();
                if (sphereMR == null)
                {
                    Debug.LogWarning("[LemmingProxyRuntimeVisualizer] Prefab has no MeshRenderer; color will not update.");
                }
                else if (materialOverride != null)
                {
                    sphereMR.sharedMaterial = materialOverride;
                }
                // Turn off shadows for clarity
                if (sphereMR) { sphereMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; sphereMR.receiveShadows = false; }
                // Remove stray colliders on the prefab
                foreach (var c in sphere.GetComponentsInChildren<Collider>()) Destroy(c);
            }
            else
            {
                Debug.LogWarning("[LemmingProxyRuntimeVisualizer] No spherePrefab assigned.");
            }

            // Create line renderers
            xLine = MakeLine("Axis_X", xColor);
            yLine = MakeLine("Axis_Y", yColor);
            zLine = MakeLine("Axis_Z", zColor);
            gazeLine = MakeLine("Gaze", gazeColor);
        }

        LineRenderer MakeLine(string name, Color c)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.widthMultiplier = lineWidth;
            lr.useWorldSpace = true;
            lr.numCapVertices = 2;
            lr.numCornerVertices = 2;
            lr.material = new Material(Shader.Find("Sprites/Default")); // simple, no custom shader
            lr.startColor = lr.endColor = c;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        void LateUpdate()
        {
            if (!sphereCol) return;

            // --- sphere size (match SphereCollider world radius) ---
            float worldRadius = sphereCol.radius * MaxAbs(transform.lossyScale);
            float worldDiameter = worldRadius * 2f;

            if (sphereT != null)
            {
                // Keep it centered on the proxy
                sphereT.localPosition = Vector3.zero;

                // Compensate (roughly) for parent scale so final world scale ≈ worldDiameter.
                float parentMax = MaxAbs(transform.lossyScale);
                float localUniform = (parentMax > 1e-5f) ? worldDiameter / parentMax : worldDiameter;
                sphereT.localScale = Vector3.one * localUniform;

                // Color by status
                if (sphereMR)
                {
                    var c = proxy.relationship ? StatusColor(proxy.relationship.Status) : colorNone;
                    mpb.SetColor("_Color", c);
                    sphereMR.SetPropertyBlock(mpb);
                }
            }

            // --- axis lines ---
            bool axesActive = drawAxes;
            xLine.gameObject.SetActive(axesActive);
            yLine.gameObject.SetActive(axesActive);
            zLine.gameObject.SetActive(axesActive);

            if (axesActive)
            {
                float len = worldRadius * Mathf.Max(0.01f, axisLengthMultiplier);
                var p = transform.position;
                SetLine(xLine, p, p + transform.right   * len);
                SetLine(yLine, p, p + transform.up      * len);
                SetLine(zLine, p, p + transform.forward * len);
            }

            // --- gaze line ---
            bool gazeActive = drawGaze;
            gazeLine.gameObject.SetActive(gazeActive);
            if (gazeActive)
            {
                var src = gazeSource ? gazeSource : GuessGazeSource();
                var origin = src ? src.position : transform.position;
                var dir = src ? src.forward : transform.forward;
                SetLine(gazeLine, origin, origin + dir.normalized * gazeLength);
            }
        }

        Transform GuessGazeSource()
        {
            // If you want “leader’s” forward, try first relationship member
            if (proxy.relationship != null && proxy.relationship.Members != null)
            {
                var first = proxy.relationship.Members.FirstOrDefault();
                if (first) return first.transform;
            }
            return transform;
        }

        static float MaxAbs(Vector3 v) => Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        static void SetLine(LineRenderer lr, Vector3 a, Vector3 b)
        {
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
        }

        Color StatusColor(RelationshipStatus s) =>
            s == RelationshipStatus.InRange ? colorInRange :
            s == RelationshipStatus.Over    ? colorOver    :
            s == RelationshipStatus.Under   ? colorUnder   :
                                              colorNone;
    }
}