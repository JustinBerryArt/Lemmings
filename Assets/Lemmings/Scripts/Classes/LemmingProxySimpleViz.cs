using UnityEngine;

namespace Lemmings
{
    /// <summary>
    /// Super-simple, runtime-only proxy visual:
    /// - Sphere prefab scaled to SphereCollider radius
    /// - Color by relationship Status
    /// - Lines for X/Y/Z axes + forward "gaze"
    /// Attach to the same GameObject as LemmingRelationshipProxy.
    /// </summary>
    [DisallowMultipleComponent]
    public class LemmingProxySimpleViz : MonoBehaviour
    {
        [Header("References")]
        public LemmingRelationshipProxy proxy;     // auto-grabbed if null
        public GameObject spherePrefab;            // required (use URP/Unlit Transparent)
        public Material lineMaterial;              // optional; falls back to Sprites/Default

        [Header("Appearance")]
        public float axisLength = 0.25f;
        public float gazeLength = 0.4f;
        public float lineWidth  = 0.01f;

        public Color inRangeColor = Color.green;
        public Color overColor    = Color.red;
        public Color underColor   = Color.yellow;
        public Color noneColor    = Color.white;

        public Color axisXColor   = Color.red;
        public Color axisYColor   = Color.green;
        public Color axisZColor   = Color.blue;
        public Color gazeColor    = new Color(1f, 0f, 1f); // magenta

        // internals
        Transform sphereTf;
        Renderer sphereRenderer;
        MaterialPropertyBlock mpb;
        LineRenderer lrX, lrY, lrZ, lrG;

        void Awake()
        {
            if (proxy == null) proxy = GetComponent<LemmingRelationshipProxy>();
            if (proxy == null || spherePrefab == null)
            {
                Debug.LogWarning("[LemmingProxySimpleViz] Missing proxy or spherePrefab.");
                enabled = false; return;
            }

            // sphere
            var sphere = Instantiate(spherePrefab, transform);
            sphereTf = sphere.transform;
            sphereRenderer = sphere.GetComponentInChildren<Renderer>();
            mpb = new MaterialPropertyBlock();

            // lines
            lrX = CreateLine("AxisX", axisXColor);
            lrY = CreateLine("AxisY", axisYColor);
            lrZ = CreateLine("AxisZ", axisZColor);
            lrG = CreateLine("Gaze",  gazeColor);
        }

        LineRenderer CreateLine(string name, Color c)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace   = true;
            lr.positionCount   = 2;
            lr.widthMultiplier = lineWidth;
            lr.numCapVertices  = 4;
            lr.material        = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lr.endColor = c;
            return lr;
        }

        void LateUpdate()
        {
            // follow proxy transform
            transform.SetPositionAndRotation(proxy.transform.position, proxy.transform.rotation);

            // sphere scale = collider radius
            var sc = proxy.GetComponent<SphereCollider>();
            float r = (sc != null ? sc.radius : 0.1f);
            sphereTf.localScale = Vector3.one * (r * 2f);

            // color by status
            var status = (proxy.relationship != null) ? proxy.relationship.Status : RelationshipStatus.None;
            Color c = status == RelationshipStatus.InRange ? inRangeColor :
                      status == RelationshipStatus.Over    ? overColor :
                      status == RelationshipStatus.Under   ? underColor : noneColor;

            if (sphereRenderer != null)
            {
                mpb.SetColor("_BaseColor", c); // URP/Unlit
                mpb.SetColor("_Color", c);     // legacy/Sprites
                sphereRenderer.SetPropertyBlock(mpb);
            }

            // axes + gaze
            var p = transform.position;
            lrX.SetPosition(0, p); lrX.SetPosition(1, p + transform.right   * axisLength);
            lrY.SetPosition(0, p); lrY.SetPosition(1, p + transform.up      * axisLength);
            lrZ.SetPosition(0, p); lrZ.SetPosition(1, p + transform.forward * axisLength);

            lrG.SetPosition(0, p);
            lrG.SetPosition(1, p + transform.forward * gazeLength);
        }
    }
}
