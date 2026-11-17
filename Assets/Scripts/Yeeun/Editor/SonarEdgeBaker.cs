using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Yeeun.Sonar;
using Yeeun.SymbolPuzzle.EditorTools;

namespace Yeeun.Sonar.EditorTools
{
    /// <summary>
    /// Bakes sonar edge lines into the open scene (needed for static objects, whose meshes
    /// Unity merges and locks when play starts). Edge meshes are saved as assets and reused.
    /// Needs a <see cref="SonarWorld"/> in the scene (its materials and settings are used).
    /// Only affects the open scene; run it again after adding or moving geometry.
    /// </summary>
    public static class SonarEdgeBaker
    {
        public const string EdgeMeshFolder = "Assets/Meshes/Yeeun/SonarEdges";

        [MenuItem("Tools/Yeeun/Sonar/Bake Edges In Open Scene")]
        static void BakeMenu()
        {
            int n = BakeOpenScene(out string message);
            Debug.Log($"[SonarEdgeBaker] {message}");
            if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        public static int BakeOpenScene(out string message)
        {
            var world = Object.FindAnyObjectByType<SonarWorld>();
            if (world == null)
            {
                message = "No SonarWorld in the open scene.";
                return 0;
            }
            SymbolMeshGenerator.EnsureFolder(EdgeMeshFolder);

            int baked = 0, skipped = 0;
            var renderers = world.WholeScene
                ? Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include)
                : world.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
            {
                var filter = r.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || r.GetComponent<SonarEdgeTag>() != null) continue;
                if (!filter.sharedMesh.isReadable) { skipped++; continue; }
                var edges = GetOrCreateEdgeAsset(filter.sharedMesh, world.CreaseAngle);
                if (world.Convert(r, edges)) baked++;
            }
            AssetDatabase.SaveAssets();
            message = $"Baked edges for {baked} renderers ({skipped} unreadable meshes skipped).";
            return baked;
        }

        static Mesh GetOrCreateEdgeAsset(Mesh source, float creaseAngle)
        {
            string safe = string.Join("_", source.name.Split(Path.GetInvalidFileNameChars()));
            string path = $"{EdgeMeshFolder}/{safe}_v{source.vertexCount}_a{Mathf.RoundToInt(creaseAngle)}_Edges.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            var built = Object.Instantiate(SonarEdgeMeshBuilder.Get(source, creaseAngle));
            built.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(built, path);
            return built;
        }
    }
}
