using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// One-click sample setup: <c>JamKit &gt; Samples &gt; Set Up …</c> automates each sample
    /// README's setup section — scaffold <c>Assets/_Project</c> if needed (the wizard without its
    /// dialogs), open or build the demo scene (camera + JamKitCore + EventSystem), add the demo
    /// component, auto-assign its services, save, press Play. Idempotent: re-running reopens the
    /// scene and re-fills references. Samples are pure runtime code the package can't reference at
    /// compile time (they exist only once imported), so demos are located by type name.
    /// </summary>
    public static class JamKitSampleSetup
    {
        public readonly struct Spec
        {
            /// <summary>Display name — also the Samples~ folder name and the demo scene name.</summary>
            public readonly string Name;
            /// <summary>Full name of the sample's single MonoBehaviour entry point.</summary>
            public readonly string TypeName;
            /// <summary>True = the demo belongs in the wizard's Game.unity instead of its own scene.</summary>
            public readonly bool InGameScene;
            /// <summary>One line logged when the sample is ready.</summary>
            public readonly string PlayHint;

            public Spec(string name, string typeName, bool inGameScene, string playHint)
            {
                Name = name;
                TypeName = typeName;
                InGameScene = inGameScene;
                PlayHint = playHint;
            }
        }

        /// <summary>
        /// Every shipped sample. Survivor Mini is the only Game.unity resident: its loop ends in the
        /// GameOver scene, whose Retry button loads the scene *named* "Game" — the demo must live
        /// there for the full loop to cycle. Everything else gets its own scene saved beside the
        /// imported sample, with a JamKitCore instance so all service runners are present.
        /// </summary>
        public static readonly Spec[] Specs =
        {
            new Spec("00 Bootstrap", "Metz.JamKit.Samples.BootstrapDemo", false,
                "Attack (left mouse / gamepad X) spawns a pooled cube and fires the click event."),
            new Spec("01 2D Platformer Mini", "Metz.JamKit.Samples.PlatformerDemo", false,
                "WASD/arrows + Space to move and jump; run into the red enemy to take damage."),
            new Spec("02 3D Walker Mini", "Metz.JamKit.Samples.WalkerDemo", false,
                "WASD to walk; collect the ring of pickups."),
            new Spec("03 UI Card Flip Mini", "Metz.JamKit.Samples.CardFlipDemo", false,
                "Click cards to flip; the best run persists across plays."),
            new Spec("04 Survivor Mini", "Metz.JamKit.Samples.SurvivorDemo", true,
                "WASD / left stick; grab the gold spheres before the countdown ends."),
            new Spec("05 Juice Toggle", "Metz.JamKit.Samples.JuiceToggleDemo", false,
                "Watch the turret, then press J to flip every juice receiver on/off."),
            new Spec("06 Arcade Playground", "Metz.JamKit.Samples.ArcadePlaygroundDemo", false,
                "WASD/arrows to cross the road, E at the lever, watch the breakout pit."),
        };

        // MenuItems must be static methods with const paths, so: one small pair per sample.
        const string MenuRoot = "JamKit/Samples/";

        [MenuItem(MenuRoot + "Set Up 00 Bootstrap", false, 41)] static void Setup00() => SetUp(Specs[0]);
        [MenuItem(MenuRoot + "Set Up 00 Bootstrap", true)] static bool Can00() => IsImported(Specs[0]);
        [MenuItem(MenuRoot + "Set Up 01 2D Platformer Mini", false, 42)] static void Setup01() => SetUp(Specs[1]);
        [MenuItem(MenuRoot + "Set Up 01 2D Platformer Mini", true)] static bool Can01() => IsImported(Specs[1]);
        [MenuItem(MenuRoot + "Set Up 02 3D Walker Mini", false, 43)] static void Setup02() => SetUp(Specs[2]);
        [MenuItem(MenuRoot + "Set Up 02 3D Walker Mini", true)] static bool Can02() => IsImported(Specs[2]);
        [MenuItem(MenuRoot + "Set Up 03 UI Card Flip Mini", false, 44)] static void Setup03() => SetUp(Specs[3]);
        [MenuItem(MenuRoot + "Set Up 03 UI Card Flip Mini", true)] static bool Can03() => IsImported(Specs[3]);
        [MenuItem(MenuRoot + "Set Up 04 Survivor Mini", false, 45)] static void Setup04() => SetUp(Specs[4]);
        [MenuItem(MenuRoot + "Set Up 04 Survivor Mini", true)] static bool Can04() => IsImported(Specs[4]);
        [MenuItem(MenuRoot + "Set Up 05 Juice Toggle", false, 46)] static void Setup05() => SetUp(Specs[5]);
        [MenuItem(MenuRoot + "Set Up 05 Juice Toggle", true)] static bool Can05() => IsImported(Specs[5]);
        [MenuItem(MenuRoot + "Set Up 06 Arcade Playground", false, 47)] static void Setup06() => SetUp(Specs[6]);
        [MenuItem(MenuRoot + "Set Up 06 Arcade Playground", true)] static bool Can06() => IsImported(Specs[6]);

        // Grayed-out entries mean "not imported yet" — give that state a door.
        [MenuItem(MenuRoot + "Import Samples (Package Manager)…", false, 60)]
        static void OpenPackageManager() => UnityEditor.PackageManager.UI.Window.Open("com.metz.jamkit");

        /// <summary>True once the sample's demo script is imported (and compiled) into the project.</summary>
        public static bool IsImported(Spec spec) => FindDemoType(spec) != null;

        static Type FindDemoType(Spec spec)
        {
            foreach (var t in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
                if (t.FullName == spec.TypeName)
                    return t;
            return null;
        }

        /// <summary>Run the sample's full README setup. Safe to re-run; never overwrites scenes.</summary>
        public static void SetUp(Spec spec)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[JamKit] Exit Play mode before setting up a sample.");
                return;
            }

            var demoType = FindDemoType(spec);
            if (demoType == null)
            {
                Debug.LogWarning($"[JamKit] Sample '{spec.Name}' isn't imported — Window > Package Manager > JamKit > Samples.");
                return;
            }

            // Opening/creating scenes below discards unsaved work without this prompt.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            bool scaffolded = false;
            if (!ProjectIsScaffolded())
            {
                JamProjectWizard.Scaffold(overwriteScenes: false);
                scaffolded = true;
            }

            string scenePath = spec.InGameScene ? JamProjectWizard.GameScenePath : OwnScenePath(spec, demoType);
            if (scenePath == null) return;

            if (File.Exists(scenePath))
                EditorSceneManager.OpenScene(scenePath);
            else
                CreateSampleScene(scenePath);

            var demo = FindOrAddDemo(demoType);
            JamKitAutoAssign.FillComponent(demo, log: true);

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = demo.gameObject;
            EditorGUIUtility.PingObject(demo.gameObject);

            ReportReady(spec, demo, scenePath, scaffolded);
        }

        /// <summary>The key wizard artifacts every sample leans on; any missing → full (idempotent) scaffold.</summary>
        static bool ProjectIsScaffolded()
            => File.Exists(JamProjectWizard.BootstrapScenePath)
            && File.Exists(JamProjectWizard.GameScenePath)
            && File.Exists(JamProjectWizard.GameOverScenePath)
            && AssetDatabase.LoadAssetAtPath<GameObject>(JamProjectWizard.CorePrefabPath) != null
            && AssetDatabase.LoadAssetAtPath<InputServiceSO>(JamProjectWizard.ServicesDir + "/InputService.asset") != null;

        /// <summary>The demo scene lives beside the imported sample, so removing the sample removes it too.</summary>
        static string OwnScenePath(Spec spec, Type demoType)
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:MonoScript {demoType.Name}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == demoType)
                    return Path.GetDirectoryName(path)?.Replace('\\', '/') + "/" + spec.Name + ".unity";
            }
            Debug.LogWarning($"[JamKit] Couldn't locate the imported script asset for {demoType.Name}.");
            return null;
        }

        /// <summary>The wizard's Game scene minus the menu: enough for any demo to run with full services.</summary>
        static void CreateSampleScene(string scenePath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            JamProjectWizard.CreateCamera(cinemachine: true);
            JamProjectWizard.CreateDirectionalLight();
            var core = AssetDatabase.LoadAssetAtPath<GameObject>(JamProjectWizard.CorePrefabPath);
            if (core != null) PrefabUtility.InstantiatePrefab(core);
            JamProjectWizard.CreateEventSystem();
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        static Component FindOrAddDemo(Type demoType)
        {
            var existing = Object.FindObjectsByType(demoType, FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (existing.Length > 0) return (Component)existing[0];

            WarnIfOtherDemoPresent(demoType);
            var go = new GameObject(demoType.Name);
            Undo.RegisterCreatedObjectUndo(go, "Set Up JamKit Sample");
            return Undo.AddComponent(go, demoType);
        }

        /// <summary>Demos each build their own scene content at runtime; two in one scene fight over it.</summary>
        static void WarnIfOtherDemoPresent(Type demoType)
        {
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (mb != null && mb.GetType() != demoType && mb.GetType().Namespace == "Metz.JamKit.Samples")
                {
                    Debug.LogWarning($"[JamKit] This scene also contains {mb.GetType().Name} — sample demos build their own content and aren't meant to run together.", mb);
                    return;
                }
        }

        static void ReportReady(Spec spec, Component demo, string scenePath, bool scaffolded)
        {
            var sb = new StringBuilder($"[JamKit] Sample ready — {spec.Name}. Press Play. {spec.PlayHint}");
            sb.Append($"\n  Scene: {scenePath}");
            if (scaffolded)
                sb.Append("\n  Scaffolded Assets/_Project first (services, prefabs, scenes — same as JamKit > New Jam Project).");
            var unassigned = ListNullReferences(demo);
            if (unassigned != null)
                sb.Append($"\n  Left empty (optional — the README shows what to wire there): {unassigned}");
            Debug.Log(sb.ToString(), demo);
        }

        /// <summary>
        /// Whatever auto-assign left null is optional by design (SFX clips, Ripple event slots for
        /// Feel wiring) — name the fields so the log doubles as the README pointer.
        /// </summary>
        static string ListNullReferences(Component demo)
        {
            var so = new SerializedObject(demo);
            var prop = so.GetIterator();
            StringBuilder sb = null;
            if (prop.NextVisible(true))
                do
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (prop.name == "m_Script" || prop.objectReferenceValue != null) continue;
                    sb = sb == null ? new StringBuilder(prop.displayName) : sb.Append(", ").Append(prop.displayName);
                } while (prop.NextVisible(false));
            return sb?.ToString();
        }
    }
}
