using System;
using System.Collections.Generic;
using Minseo.MazeItems.Tests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Minseo.MazeItems.Editor
{
    internal static class ItemTestSceneBuilder
    {
        private const string Root = "Assets/Minseo";
        private const string Prefabs = Root + "/Prefabs";
        private const string Materials = Root + "/Materials";
        private const string ScenePath = Root + "/Scenes/ItemTest.unity";

        [MenuItem("Tools/Minseo/Open Item Test Scene")]
        private static void OpenTestScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene previous = SceneManager.GetActiveScene();
            bool replaceBlankScene = SceneManager.sceneCount == 1 && string.IsNullOrEmpty(previous.path) && !previous.isDirty;
            if (string.IsNullOrEmpty(previous.path) && !replaceBlankScene)
            {
                Debug.LogWarning("Save the untitled scene before opening the private item test scene.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                CreateAssets(previous, replaceBlankScene);

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, replaceBlankScene ? OpenSceneMode.Single : OpenSceneMode.Additive);

            SceneManager.SetActiveScene(scene);
            Selection.activeObject = scene.GetRootGameObjects()[0];
        }

        private static void CreateAssets(Scene previous, bool replaceBlankScene)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("The project URP Unlit shader is required to build the test assets.");

            EnsureFolder("Assets", "Minseo");
            EnsureFolder(Root, "Prefabs");
            EnsureFolder(Root, "Materials");
            EnsureFolder(Root, "Scenes");

            Scene temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                replaceBlankScene ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(temporary);
            try
            {
                Material green = CreateMaterial(shader, "MarkerGreen", new Color(0.05f, 0.95f, 0.28f));
                Material red = CreateMaterial(shader, "HealthKitRed", new Color(0.85f, 0.08f, 0.12f));
                Material amber = CreateMaterial(shader, "BeaconAmber", new Color(0.95f, 0.6f, 0.05f));
                Material wall = CreateMaterial(shader, "TestWall", new Color(0.12f, 0.17f, 0.23f));
                Material floor = CreateMaterial(shader, "TestFloor", new Color(0.035f, 0.055f, 0.08f));

                GameObject marker = GetOrCreatePrefab("PlacedBeacon", green, new Vector3(0.23f, 0.40f, 0.23f));
                GameObject kit = GetOrCreatePrefab("HealthKit", red, new Vector3(0.36f, 0.25f, 0.27f), typeof(HealthKitPickup));
                GameObject pickup = GetOrCreatePrefab("BeaconPickup", amber, new Vector3(0.24f, 0.28f, 0.24f),
                    typeof(BeaconPickup), marker);

                var maze = new GameObject("Test Maze");
                var surfaces = new List<Renderer>();
                void Block(string name, Vector3 position, Vector3 size, Material material)
                {
                    GameObject block = CreatePrimitive(name, position, size, material);
                    block.transform.SetParent(maze.transform, true);
                    surfaces.Add(block.GetComponent<Renderer>());
                }

                Block("Branch floor", new Vector3(0, -0.1f, 0), new Vector3(18, 0.2f, 3), floor);
                Block("Entry floor", new Vector3(0, -0.1f, -4.25f), new Vector3(3, 0.2f, 5.5f), floor);
                Block("North occluder", new Vector3(0, 1.4f, 1.65f), new Vector3(18, 2.8f, 0.3f), wall);
                Block("East branch wall", new Vector3(5.25f, 1.4f, -1.65f), new Vector3(7.5f, 2.8f, 0.3f), wall);
                Block("West branch wall", new Vector3(-5.25f, 1.4f, -1.65f), new Vector3(7.5f, 2.8f, 0.3f), wall);
                Block("Entry west wall", new Vector3(-1.65f, 1.4f, -4.25f), new Vector3(0.3f, 2.8f, 5.5f), wall);
                Block("Entry east wall", new Vector3(1.65f, 1.4f, -4.25f), new Vector3(0.3f, 2.8f, 5.5f), wall);
                Block("Entry end wall", new Vector3(0, 1.4f, -7.15f), new Vector3(3.6f, 2.8f, 0.3f), wall);
                Block("West end wall", new Vector3(-9.15f, 1.4f, 0), new Vector3(0.3f, 2.8f, 3.6f), wall);
                Block("East end wall", new Vector3(9.15f, 1.4f, 0), new Vector3(0.3f, 2.8f, 3.6f), wall);

                var player = new GameObject("Item Test Player");
                player.transform.position = new Vector3(0, 0, -6);
                var testPlayer = player.AddComponent<ItemTestPlayer>();
                var controller = player.GetComponent<CharacterController>();
                controller.center = Vector3.up * 0.9f;
                controller.height = 1.8f;
                controller.radius = 0.3f;
                controller.minMoveDistance = 0;

                var cameraObject = new GameObject("Item Test Camera");
                cameraObject.transform.SetParent(player.transform, false);
                cameraObject.transform.localPosition = Vector3.up * 1.5f;
                var camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 35;
                camera.depth = 100;

                var playerSetup = new SerializedObject(testPlayer);
                playerSetup.FindProperty("view").objectReferenceValue = camera;
                SerializedProperty surfaceList = playerSetup.FindProperty("mazeSurfaces");
                surfaceList.arraySize = surfaces.Count;
                for (int index = 0; index < surfaces.Count; index++)
                    surfaceList.GetArrayElementAtIndex(index).objectReferenceValue = surfaces[index];
                playerSetup.ApplyModifiedPropertiesWithoutUndo();
                Assign(player.GetComponent<PlayerItems>(), "healingTarget", testPlayer);

                var checks = new GameObject("Item Play Mode Checks").AddComponent<ItemTestScenario>();
                Assign(checks, "player", testPlayer);
                Assign(checks, "firstKit", PlacePrefab(kit, temporary, -0.85f, -4.5f));
                Assign(checks, "secondKit", PlacePrefab(kit, temporary, 0.85f, -4.5f));
                Assign(checks, "firstBeacon", PlacePrefab(pickup, temporary, -0.85f, -2.6f));
                Assign(checks, "secondBeacon", PlacePrefab(pickup, temporary, 0.85f, -2.6f));

                if (!EditorSceneManager.SaveScene(temporary, ScenePath))
                    throw new InvalidOperationException("Could not save the private item test scene.");
            }
            finally
            {
                if (!replaceBlankScene)
                {
                    SceneManager.SetActiveScene(previous);
                    EditorSceneManager.CloseScene(temporary, true);
                }
            }
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static Material CreateMaterial(Shader shader, string name, Color color)
        {
            string path = Materials + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            var material = new Material(shader);
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject GetOrCreatePrefab(string name, Material material, Vector3 size,
            Type pickupType = null, GameObject marker = null)
        {
            string path = Prefabs + "/" + name + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            var root = new GameObject(name);
            GameObject mesh = CreatePrimitive("Visual", Vector3.up * size.y * 0.5f, size, material);
            mesh.transform.SetParent(root.transform, false);
            UnityEngine.Object.DestroyImmediate(mesh.GetComponent<Collider>());

            if (pickupType != null)
            {
                Component pickup = root.AddComponent(pickupType);
                var collider = root.GetComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.center = Vector3.up * 0.22f;
                collider.radius = 0.42f;
                var body = root.GetComponent<Rigidbody>();
                body.useGravity = false;
                body.isKinematic = true;
                if (marker != null)
                    Assign(pickup, "placedMarkerPrefab", marker);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreatePrimitive(string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = name;
            mesh.transform.SetPositionAndRotation(position, Quaternion.identity);
            mesh.transform.localScale = size;
            var renderer = mesh.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return mesh;
        }

        private static GameObject PlacePrefab(GameObject prefab, Scene scene, float x, float z)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.position = new Vector3(x, 0, z);
            return instance;
        }

        private static void Assign(Component component, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(component);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
