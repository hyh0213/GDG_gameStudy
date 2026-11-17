using System;
using System.IO;
using DarkMaze.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DarkMaze.Editor
{
    /// <summary>Original, hand-drawn cursor artwork. Writes only new personal assets.</summary>
    public static class MenuCursorBuilder
    {
        public const string Folder = "Assets/UI/하윤형/Art/Cursors";
        private static readonly Color Outline = new Color32(10, 19, 20, 255);
        private static readonly Color Ivory = new Color32(239, 233, 210, 255);
        private static readonly Color Gold = new Color32(196, 165, 108, 255);

        [MenuItem("Tools/하윤형/UI/Add Menu Cursor")]
        public static void AddCursor()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            if (Directory.Exists(Folder) || File.Exists(Folder + ".meta"))
                throw new InvalidOperationException("Cursor art already exists; it will not be overwritten.");
            GameObject root = PrefabUtility.LoadPrefabContents(StartMenuSceneBuilder.PrefabPath);
            try
            {
                if (root.GetComponentInChildren<MenuCursorController>(true) != null)
                    throw new InvalidOperationException("The personal menu already has a cursor controller.");
                MainMenuController menu = root.GetComponentInChildren<MainMenuController>(true);
                if (menu == null) throw new InvalidOperationException("The personal menu controller is missing.");
                AssetDatabase.CreateFolder("Assets/UI/하윤형/Art", "Cursors");
                Texture2D arrow = CreateTexture("MenuArrow", new[] {
                    new Vector2(4,2), new Vector2(4,26), new Vector2(10,20), new Vector2(15,30),
                    new Vector2(21,27), new Vector2(16,18), new Vector2(27,18)
                }, false);
                Texture2D select = CreateTexture("MenuSelect", new[] {
                    new Vector2(10,16), new Vector2(10,5), new Vector2(11,3), new Vector2(14,3),
                    new Vector2(15,5), new Vector2(15,12), new Vector2(17,10), new Vector2(20,10),
                    new Vector2(21,12), new Vector2(24,11), new Vector2(27,13), new Vector2(28,19),
                    new Vector2(27,25), new Vector2(23,29), new Vector2(15,29), new Vector2(12,26),
                    new Vector2(5,18), new Vector2(5,15), new Vector2(7,14)
                }, true);
                MenuCursorController cursor = menu.gameObject.AddComponent<MenuCursorController>();
                using (var so = new SerializedObject(cursor))
                {
                    so.FindProperty("arrowTexture").objectReferenceValue = arrow;
                    so.FindProperty("selectTexture").objectReferenceValue = select;
                    so.FindProperty("arrowHotspot").vector2Value = new Vector2(4, 2);
                    so.FindProperty("selectHotspot").vector2Value = new Vector2(12, 3);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, StartMenuSceneBuilder.PrefabPath);
                CreatePreview(arrow, select);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static Texture2D CreateTexture(string name, Vector2[] polygon, bool hand)
        {
            const int size = 32;
            const int samples = 4;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector3 rgb = Vector3.zero;
                float alpha = 0;
                for (int sy = 0; sy < samples; sy++)
                for (int sx = 0; sx < samples; sx++)
                {
                    Vector2 point = new Vector2(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples);
                    float edge = EdgeDistance(point, polygon);
                    Color pixel = Color.clear;
                    if (Inside(point, polygon))
                    {
                        pixel = edge < 1.2f ? Outline : Ivory;
                        if (edge >= 1.2f && (hand ? point.y > 25.5f : point.x < 7f && point.y > 8f)) pixel = Gold;
                        if (hand && edge >= 1.2f && point.y > 14 && point.y < 21 &&
                            (Mathf.Abs(point.x - 16.5f) < 0.5f || Mathf.Abs(point.x - 21.5f) < 0.5f)) pixel = Gold;
                    }
                    rgb += new Vector3(pixel.r, pixel.g, pixel.b) * pixel.a;
                    alpha += pixel.a;
                }
                Color result = alpha > 0 ? new Color(rgb.x / alpha, rgb.y / alpha, rgb.z / alpha,
                    alpha / (samples * samples)) : Color.clear;
                texture.SetPixel(x, size - 1 - y, result); // PNG origin differs from cursor hotspot's top-left origin.
            }
            texture.Apply();
            string path = Folder + "/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Cursor;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static bool Inside(Vector2 p, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i], b = polygon[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        private static float EdgeDistance(Vector2 p, Vector2[] polygon)
        {
            float distance = float.MaxValue;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Length];
                Vector2 segment = b - a;
                Vector2 closest = a + segment * Mathf.Clamp01(Vector2.Dot(p - a, segment) / segment.sqrMagnitude);
                distance = Mathf.Min(distance, Vector2.Distance(p, closest));
            }
            return distance;
        }

        private static void CreatePreview(Texture2D arrow, Texture2D select)
        {
            const int scale = 4;
            var preview = new Texture2D(360, 168, TextureFormat.RGB24, false);
            Color background = new Color32(17, 28, 29, 255);
            for (int y = 0; y < preview.height; y++)
            for (int x = 0; x < preview.width; x++) preview.SetPixel(x, y, background);
            Texture2D[] textures = { arrow, select };
            for (int i = 0; i < textures.Length; i++)
            for (int y = 0; y < 32 * scale; y++)
            for (int x = 0; x < 32 * scale; x++)
            {
                Color foreground = textures[i].GetPixel(x / scale, y / scale);
                preview.SetPixel(20 + i * 180 + x, 20 + y, Color.Lerp(background, foreground, foreground.a));
            }
            preview.Apply();
            Directory.CreateDirectory("Temp");
            File.WriteAllBytes("Temp/HYH-Cursors.png", preview.EncodeToPNG());
            Object.DestroyImmediate(preview);
        }
    }
}
