using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yeeun.Player;
using Yeeun.Effects;

namespace Yeeun.EditorTools
{
    public static class MazeMovementSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Yeeun/MazeMovementTest.unity";
        public const string PrefabPath = "Assets/Prefabs/Yeeun/Player/MazePlayer.prefab";
        [MenuItem("Yeeun/Player/Create Movement Test (new assets only)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your open scenes first.");
            if (File.Exists(ScenePath) || File.Exists(PrefabPath))
                throw new InvalidOperationException("Test assets already exist; refusing to overwrite.");
            Folder("Assets/Scenes/Yeeun"); Folder("Assets/Prefabs/Yeeun/Player");
            Folder("Assets/Materials/Yeeun/Movement"); Folder("Assets/Audio/Yeeun/Movement");
            var floorMat = Material("Floor", new Color(0.20f, 0.25f, 0.29f));
            var wallMat = Material("Wall", new Color(0.42f, 0.51f, 0.55f));
            var accent = Material("Landmark", new Color(0.85f, 0.51f, 0.15f));
            var clip = CreateStep();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Yeeun Maze Player");
            var body = root.AddComponent<CharacterController>();
            body.height = 1.8f; body.radius = 0.28f; body.center = Vector3.up * 0.9f;
            body.skinWidth = 0.025f; body.stepOffset = 0.22f; body.minMoveDistance = 0;
            var look = Child("Look", root.transform, new Vector3(0, 1.62f, 0));
            var gait = Child("Gait", look, Vector3.zero);
            var impact = Child("Impact", gait, Vector3.zero);
            var camTransform = Child("Player Camera", impact, Vector3.zero);
            var camera = camTransform.gameObject.AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.nearClipPlane = 0.04f; camera.farClipPlane = 80;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.07f, 0.09f, 0.12f);
            camTransform.gameObject.AddComponent<AudioListener>();
            var shaker = impact.gameObject.AddComponent<CameraShaker>();
            var audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false; audio.spatialBlend = 0; audio.volume = 1;
            var player = root.AddComponent<MazePlayerController>();
            player.lookPivot = look; player.gaitPivot = gait; player.viewCamera = camera;
            player.footsteps = audio; player.footstepClips = new[] { clip };
            var feedback = root.AddComponent<PlayerImpactFeedback>(); feedback.impactPivot = shaker;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            root.transform.position = new Vector3(0, 0.05f, 0);
            player = root.GetComponent<MazePlayerController>();
            Box("Floor", new Vector3(0, -0.15f, 4), new Vector3(24, 0.3f, 24), floorMat);
            // 1.8 m clear corridor, a T junction and a second turn on the right.
            Box("Start backstop", new Vector3(0, 1.4f, -2), new Vector3(2.2f, 2.8f, 0.4f), wallMat);
            Box("Left corridor wall", new Vector3(-1.1f, 1.4f, 2.5f), new Vector3(0.4f, 2.8f, 9), wallMat);
            Box("Right corridor wall", new Vector3(1.1f, 1.4f, 2.5f), new Vector3(0.4f, 2.8f, 9), wallMat);
            Box("Junction far wall", new Vector3(0, 1.4f, 9), new Vector3(16, 2.8f, 0.4f), wallMat);
            Box("Left branch inner wall", new Vector3(-4.5f, 1.4f, 6.8f), new Vector3(6.4f, 2.8f, 0.4f), wallMat);
            Box("Right branch inner wall", new Vector3(4.5f, 1.4f, 6.8f), new Vector3(6.4f, 2.8f, 0.4f), wallMat);
            Box("Left dead end", new Vector3(-8, 1.4f, 8), new Vector3(0.4f, 2.8f, 2.4f), wallMat);
            Box("Second turn outer wall", new Vector3(9, 1.4f, 5), new Vector3(0.4f, 2.8f, 8), wallMat);
            Box("Second turn inner wall", new Vector3(7, 1.4f, 3.5f), new Vector3(0.4f, 2.8f, 5), wallMat);
            Box("Finish", new Vector3(8, 0.015f, 2), new Vector3(1.5f, 0.03f, 1), accent);
            for (int i = 0; i < 9; i++)
                Box("Distance stripe " + i, new Vector3(0, 0.005f, i), new Vector3(0.75f, 0.01f, 0.045f), accent);
            var probe = Box("Monster hearing probe (6 m from start)", new Vector3(5, 0.4f, 3), new Vector3(0.5f, 0.8f, 0.5f), accent);
            var receiver = probe.AddComponent<FootstepHearingReceiver>();
            var demo = new GameObject("Test controls and hearing display").AddComponent<MazeMovementDemo>();
            demo.player = player; demo.impacts = root.GetComponent<PlayerImpactFeedback>(); demo.receiver = receiver;
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = root;
        }
        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            Folder(parent); AssetDatabase.CreateFolder(parent, path.Substring(parent.Length + 1));
        }
        static Transform Child(string name, Transform parent, Vector3 local)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = local; return t;
        }
        static Material Material(string name, Color color)
        {
            string path = "Assets/Materials/Yeeun/Movement/" + name + ".mat";
            if (File.Exists(path)) throw new InvalidOperationException("Refusing to overwrite " + path);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); mat.color = color;
            AssetDatabase.CreateAsset(mat, path); return mat;
        }
        static GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        static AudioClip CreateStep()
        {
            string path = "Assets/Audio/Yeeun/Movement/PrototypeFootstep.wav";
            if (File.Exists(path)) throw new InvalidOperationException("Refusing to overwrite " + path);
            const int rate = 22050, count = 4410;
            var random = new System.Random(751);
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
                for (int i = 0; i < count; i++)
                {
                    double t = (double)i / rate;
                    double envelope = Math.Min(1, t / 0.005) * Math.Exp(-t * 28);
                    double signal = (Math.Sin(2 * Math.PI * 85 * t) * 0.55 + (random.NextDouble() * 2 - 1) * 0.25) * envelope;
                    writer.Write((short)(signal * 26000));
                }
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
