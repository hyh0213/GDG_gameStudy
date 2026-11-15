using UnityEngine;
using UnityEngine.Rendering;
using Yeeun.SymbolPuzzle;

namespace Yeeun.Sonar
{
    /// <summary>
    /// Demo "sonar view" without a post-process: at start, every mesh in the scene is drawn
    /// pure black (so it still blocks what is behind it) and gets a child with its edge lines,
    /// which only light up where the device's sound has reached (see SonarEdge shader).
    /// Interactive things (puzzle symbols, items) get a brighter line colour.
    /// Runtime only: nothing in the scene file changes.
    /// Temporary approach until the team decides on the renderer (a post-process outline
    /// would make the edge meshes unnecessary).
    /// </summary>
    public class SonarWorld : MonoBehaviour
    {
        public const string EdgeChildName = "SonarEdges";

        [SerializeField] Material occluderMaterial;
        [SerializeField] Material edgeMaterial;
        [SerializeField] Material highlightEdgeMaterial;
        [Tooltip("Edges between faces bent more than this become lines.")]
        [SerializeField, Range(1f, 89f)] float creaseAngle = 30f;
        [Tooltip("Convert every mesh in the scene (otherwise only children of this object).")]
        [SerializeField] bool wholeScene = true;

        public int ConvertedCount { get; private set; }
        public int EdgeCount { get; private set; }
        public int SkippedUnreadable { get; private set; }
        public float CreaseAngle => creaseAngle;
        public bool WholeScene => wholeScene;

        // Static walls are merged by Unity's static batching when play starts and can no
        // longer be read, so their edges must be baked in the editor beforehand
        // (Tools > Yeeun > Sonar > Bake Edges In Open Scene). Start only converts the rest.
        void Start() => ConvertAll();

        public void ConvertAll()
        {
            var renderers = wholeScene
                ? FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include)
                : GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers) Convert(r);
        }

        /// <summary>
        /// Converts one renderer (also usable for objects spawned later). Pass a pre-built
        /// edge mesh when baking in the editor; otherwise it is built from the renderer's mesh.
        /// Returns false when skipped.
        /// </summary>
        public bool Convert(MeshRenderer r, Mesh bakedEdges = null)
        {
            if (r == null || r.GetComponent<SonarEdgeTag>() != null || r.GetComponentInParent<SonarIgnore>() != null) return false;
            var filter = r.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || r.transform.Find(EdgeChildName) != null) return false;
            if (bakedEdges == null && !filter.sharedMesh.isReadable)
            {
                SkippedUnreadable++;
                if (SkippedUnreadable == 1)
                    Debug.LogWarning($"[SonarWorld] '{r.name}' uses an unreadable mesh ({filter.sharedMesh.name}), " +
                                     "probably static batching. Bake edges in the editor: Tools > Yeeun > Sonar > Bake Edges In Open Scene.");
                return false;
            }

            var mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
            for (int i = 0; i < mats.Length; i++) mats[i] = occluderMaterial;
            r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.Off;

            var go = new GameObject(EdgeChildName);
            go.layer = r.gameObject.layer;
            go.transform.SetParent(r.transform, false);
            go.AddComponent<SonarEdgeTag>();
            var edges = bakedEdges != null ? bakedEdges : SonarEdgeMeshBuilder.Get(filter.sharedMesh, creaseAngle);
            go.AddComponent<MeshFilter>().sharedMesh = edges;
            var er = go.AddComponent<MeshRenderer>();
            er.sharedMaterial = IsHighlighted(r) ? highlightEdgeMaterial : edgeMaterial;
            er.shadowCastingMode = ShadowCastingMode.Off;
            er.receiveShadows = false;

            ConvertedCount++;
            EdgeCount += SonarEdgeMeshBuilder.EdgeCount(edges);
            return true;
        }

        static bool IsHighlighted(MeshRenderer r) =>
            r.GetComponentInParent<ISymbolInteractable>() != null || r.GetComponentInParent<SymbolVisual>() != null;
    }
}
