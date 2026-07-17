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
    /// One-click sample setup: <c>JamKit &gt; Samples &gt; Set Up …</c>. Samples are prefab-first —
    /// each ships a scene of prefab instances plus its prefabs (service slots deliberately null,
    /// because package assets can never reference project assets). Setup closes that gap:
    /// scaffold <c>Assets/_Project</c> if needed, wire every imported sample PREFAB to the
    /// project's services (asset-level, so all instances inherit it), open the shipped scene,
    /// drop in the project's JamKitCore, fill the rest, save. Idempotent.
    /// </summary>
    public static class JamKitSampleSetup
    {
        public readonly struct Spec
        {
            /// <summary>Display name — also the sample folder name and (for own-scene samples) the scene name.</summary>
            public readonly string Name;
            /// <summary>True = the sample's arena prefab is instanced into the wizard's Game.unity instead of its own scene.</summary>
            public readonly bool InGameScene;
            /// <summary>For InGameScene samples: the prefab (by name, without extension) dropped into Game.unity.</summary>
            public readonly string ArenaPrefab;
            /// <summary>One line logged when the sample is ready.</summary>
            public readonly string PlayHint;

            public Spec(string name, bool inGameScene, string arenaPrefab, string playHint)
            {
                Name = name;
                InGameScene = inGameScene;
                ArenaPrefab = arenaPrefab;
                PlayHint = playHint;
            }
        }

        /// <summary>
        /// Every shipped sample. Survivor is the only Game.unity resident: its loop ends in the
        /// GameOver scene, whose Retry button loads the scene *named* "Game" — the arena must
        /// live there for the full loop to cycle.
        /// </summary>
        public static readonly Spec[] Specs =
        {
            new Spec("00 Hour Zero", false, null,
                "The kit tour: services, pooling, Ripple events, HUD binding — read the hierarchy, everything is a prefab."),
            new Spec("01 Platformer", false, null,
                "WASD/arrows + Space; hazards hurt, the pit kills, the flag respawns you."),
            new Spec("02 Survivor", true, "SurvivorArena",
                "WASD / left stick; survive the chasers, grab pickups before the timer ends."),
            new Spec("03 Feel Showcase", false, null,
                "Requires the Feel asset. Click Damage on a target's Health to watch the wired MMF_Player stacks fire."),
            new Spec("04 Arcade", false, null,
                "Pong + breakout, built from the sample-local arcade components (Bouncer2D, Paddle, GridMover…)."),
        };

        const string MenuRoot = "JamKit/Samples/";

        [MenuItem(MenuRoot + "Set Up 00 Hour Zero", false, 41)] static void Setup00() => SetUp(Specs[0]);
        [MenuItem(MenuRoot + "Set Up 00 Hour Zero", true)] static bool Can00() => IsImported(Specs[0]);
        [MenuItem(MenuRoot + "Set Up 01 Platformer", false, 42)] static void Setup01() => SetUp(Specs[1]);
        [MenuItem(MenuRoot + "Set Up 01 Platformer", true)] static bool Can01() => IsImported(Specs[1]);
        [MenuItem(MenuRoot + "Set Up 02 Survivor", false, 43)] static void Setup02() => SetUp(Specs[2]);
        [MenuItem(MenuRoot + "Set Up 02 Survivor", true)] static bool Can02() => IsImported(Specs[2]);
        [MenuItem(MenuRoot + "Set Up 03 Feel Showcase", false, 44)] static void Setup03() => SetUp(Specs[3]);
        [MenuItem(MenuRoot + "Set Up 03 Feel Showcase", true)] static bool Can03() => IsImported(Specs[3]);
        [MenuItem(MenuRoot + "Set Up 04 Arcade", false, 45)] static void Setup04() => SetUp(Specs[4]);
        [MenuItem(MenuRoot + "Set Up 04 Arcade", true)] static bool Can04() => IsImported(Specs[4]);

        // Grayed-out entries mean "not imported yet" — give that state a door.
        [MenuItem(MenuRoot + "Import Samples (Package Manager)…", false, 60)]
        static void OpenPackageManager() => UnityEditor.PackageManager.UI.Window.Open("com.metz.jamkit");

        /// <summary>True once the sample folder (with its scene) is imported into Assets/Samples.</summary>
        public static bool IsImported(Spec spec) => FindSampleFolder(spec) != null;

        /// <summary>Imported sample root, located by its shipped "&lt;Name&gt;/&lt;Name&gt;.unity" scene.</summary>
        static string FindSampleFolder(Spec spec)
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:SceneAsset {spec.Name}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith($"/{spec.Name}/{spec.Name}.unity"))
                    return Path.GetDirectoryName(path)?.Replace('\\', '/');
            }
            return null;
        }

        /// <summary>Run the sample's full setup. Safe to re-run; never overwrites shipped content.</summary>
        public static void SetUp(Spec spec)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[JamKit] Exit Play mode before setting up a sample.");
                return;
            }

            var folder = FindSampleFolder(spec);
            if (folder == null)
            {
                Debug.LogWarning($"[JamKit] Sample '{spec.Name}' isn't imported — Window > Package Manager > JamKit > Samples.");
                return;
            }

            // Opening scenes below discards unsaved work without this prompt.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            bool scaffolded = false;
            if (!ProjectIsScaffolded())
            {
                JamProjectWizard.Scaffold(overwriteScenes: false);
                scaffolded = true;
            }

            // Wire the imported prefab ASSETS to the project's services — this is the step the
            // package-immutability wall makes necessary, and asset-level wiring means every
            // instance everywhere (shipped scene included) inherits the fix.
            int wired = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
                wired += JamKitAutoAssign.FillPrefabAsset(AssetDatabase.GUIDToAssetPath(guid));

            string scenePath;
            if (spec.InGameScene)
            {
                scenePath = JamProjectWizard.GameScenePath;
                EditorSceneManager.OpenScene(scenePath);
                InstantiateArenaIfAbsent(spec, folder);
            }
            else
            {
                scenePath = $"{folder}/{spec.Name}.unity";
                EditorSceneManager.OpenScene(scenePath);
                InstantiateCoreIfAbsent();
            }

            JamKitAutoAssign.FillOpenScenes();

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            ReportReady(spec, scenePath, scaffolded, wired);
        }

        /// <summary>The key wizard artifacts every sample leans on; any missing → full (idempotent) scaffold.</summary>
        static bool ProjectIsScaffolded()
            => File.Exists(JamProjectWizard.BootstrapScenePath)
            && File.Exists(JamProjectWizard.GameScenePath)
            && File.Exists(JamProjectWizard.GameOverScenePath)
            && AssetDatabase.LoadAssetAtPath<GameObject>(JamProjectWizard.CorePrefabPath) != null
            && AssetDatabase.LoadAssetAtPath<InputServiceSO>(JamProjectWizard.ServicesDir + "/InputService.asset") != null;

        /// <summary>Shipped sample scenes carry no JamKitCore (it's a project asset) — instance the project's.</summary>
        static void InstantiateCoreIfAbsent()
        {
            if (Object.FindObjectsByType<SceneServiceRunner>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
                return;
            var core = AssetDatabase.LoadAssetAtPath<GameObject>(JamProjectWizard.CorePrefabPath);
            if (core != null) PrefabUtility.InstantiatePrefab(core);
        }

        static void InstantiateArenaIfAbsent(Spec spec, string folder)
        {
            if (string.IsNullOrEmpty(spec.ArenaPrefab)) return;
            var path = $"{folder}/Prefabs/{spec.ArenaPrefab}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[JamKit] Arena prefab not found at {path}.");
                return;
            }
            foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (PrefabUtility.GetCorrespondingObjectFromSource(go.gameObject) == prefab)
                    return; // already placed
            PrefabUtility.InstantiatePrefab(prefab);
        }

        static void ReportReady(Spec spec, string scenePath, bool scaffolded, int wired)
        {
            var sb = new StringBuilder($"[JamKit] Sample ready — {spec.Name}. Press Play. {spec.PlayHint}");
            sb.Append($"\n  Scene: {scenePath}");
            if (wired > 0) sb.Append($"\n  Wired {wired} service reference(s) into the sample's prefabs.");
            if (scaffolded)
                sb.Append("\n  Scaffolded Assets/_Project first (services, prefabs, scenes — same as JamKit > New Jam Project).");
            Debug.Log(sb.ToString());
        }
    }
}
