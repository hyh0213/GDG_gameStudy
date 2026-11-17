using System.Collections.Generic;
using UnityEngine;

namespace Yeeun.Sonar
{
    /// <summary>
    /// Turns a mesh into an "edge mesh" for the SonarEdge shader: one quad per feature edge
    /// (outer border, or a crease sharper than the given angle). Flat faces split into
    /// triangles and smooth curved surfaces produce no lines. Results are cached per mesh.
    /// Vertex data: position = this endpoint, uv0 = (side -1/+1, +1 start / -1 end),
    /// uv1 = the other endpoint.
    /// </summary>
    public static class SonarEdgeMeshBuilder
    {
        static readonly Dictionary<(Mesh, int), Mesh> Cache = new Dictionary<(Mesh, int), Mesh>();

        struct EdgeInfo
        {
            public int a, b, count;
            public Vector3 normal;
            public bool crease;
        }

        public static Mesh Get(Mesh source, float creaseAngle)
        {
            var key = (source, Mathf.RoundToInt(creaseAngle * 10f));
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var built = Build(source, creaseAngle);
            Cache[key] = built;
            return built;
        }

        /// <summary>Number of edges in an edge mesh (4 vertices per edge).</summary>
        public static int EdgeCount(Mesh edgeMesh) => edgeMesh == null ? 0 : edgeMesh.vertexCount / 4;

        static Mesh Build(Mesh source, float creaseAngle)
        {
            Vector3[] verts = source.vertices;
            int[] tris = source.triangles;

            // Weld vertices that share a position (hard edges duplicate them).
            var welded = new List<Vector3>();
            var lookup = new Dictionary<Vector3Int, int>();
            var remap = new int[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                var k = Vector3Int.RoundToInt(verts[i] * 10000f);
                if (!lookup.TryGetValue(k, out int idx))
                {
                    idx = welded.Count;
                    welded.Add(verts[i]);
                    lookup.Add(k, idx);
                }
                remap[i] = idx;
            }

            var edges = new Dictionary<long, EdgeInfo>();
            for (int t = 0; t + 2 < tris.Length; t += 3)
            {
                int i0 = remap[tris[t]], i1 = remap[tris[t + 1]], i2 = remap[tris[t + 2]];
                if (i0 == i1 || i1 == i2 || i0 == i2) continue;
                Vector3 n = Vector3.Cross(welded[i1] - welded[i0], welded[i2] - welded[i0]);
                if (n.sqrMagnitude < 1e-12f) continue;
                n.Normalize();
                AddEdge(edges, i0, i1, n, creaseAngle);
                AddEdge(edges, i1, i2, n, creaseAngle);
                AddEdge(edges, i2, i0, n, creaseAngle);
            }

            var pos = new List<Vector3>();
            var uv0 = new List<Vector2>();
            var uv1 = new List<Vector3>();
            var idxs = new List<int>();
            foreach (var e in edges.Values)
            {
                if (!(e.crease || e.count == 1 || e.count > 2)) continue;
                Vector3 a = welded[e.a], b = welded[e.b];
                int s = pos.Count;
                pos.Add(a); uv0.Add(new Vector2(-1, 1)); uv1.Add(b);
                pos.Add(a); uv0.Add(new Vector2(1, 1)); uv1.Add(b);
                pos.Add(b); uv0.Add(new Vector2(-1, -1)); uv1.Add(a);
                pos.Add(b); uv0.Add(new Vector2(1, -1)); uv1.Add(a);
                idxs.Add(s); idxs.Add(s + 1); idxs.Add(s + 2);
                idxs.Add(s + 2); idxs.Add(s + 1); idxs.Add(s + 3);
            }

            var mesh = new Mesh { name = source.name + "_SonarEdges" };
            if (pos.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(pos);
            mesh.SetUVs(0, uv0);
            mesh.SetUVs(1, uv1);
            mesh.SetTriangles(idxs, 0);
            var bounds = source.bounds;
            bounds.Expand(0.1f);
            mesh.bounds = bounds;
            return mesh;
        }

        static void AddEdge(Dictionary<long, EdgeInfo> edges, int x, int y, Vector3 normal, float creaseAngle)
        {
            int lo = Mathf.Min(x, y), hi = Mathf.Max(x, y);
            long key = ((long)lo << 32) | (uint)hi;
            if (edges.TryGetValue(key, out var e))
            {
                e.count++;
                if (Vector3.Angle(e.normal, normal) > creaseAngle) e.crease = true;
                edges[key] = e;
            }
            else
            {
                edges.Add(key, new EdgeInfo { a = lo, b = hi, count = 1, normal = normal });
            }
        }
    }
}
