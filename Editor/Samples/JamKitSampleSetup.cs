using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Editor
{
    public static class JamKitSampleSetup
    {
        public readonly struct Spec
        {
            public readonly string Name;
            public readonly bool InGameScene;
            public readonly string ArenaPrefab;
            public readonly string PlayHint;

            public Spec(string name, bool inGameScene, string arenaPrefab, string playHint)
            {
                Name = name;
                InGameScene = inGameScene;
                ArenaPrefab = arenaPrefab;
                PlayHint = playHint;
            }
        }

        public static readonly Spec[] Specs =
        {
            new Spec("00 Hour Zero", false, null,
                "The kit tour: services, Ripple events, HUD binding — read the hierarchy, everything is a prefab."),
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

        [MenuItem(MenuRoot + "Import Samples (Package Manager)…", false, 60)]
        static void OpenPackageManager() => UnityEditor.PackageManager.UI.Window.Open("com.metz.jamkit");

        public static bool IsImported(Spec spec) => FindSampleFolder(spec) != null;

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

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            bool scaffolded = false;
            if (!ProjectIsScaffolded())
            {
                JamProjectWizard.Scaffold(overwriteScenes: false);
                scaffolded = true;
            }

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

        static bool ProjectIsScaffolded()
            => File.Exists(JamProjectWizard.BootstrapScenePath)
            && File.Exists(JamProjectWizard.GameScenePath)
            && File.Exists(JamProjectWizard.GameOverScenePath)
            && AssetDatabase.LoadAssetAtPath<GameObject>(JamProjectWizard.CorePrefabPath) != null
            && AssetDatabase.LoadAssetAtPath<InputServiceSO>(JamProjectWizard.ServicesDir + "/InputService.asset") != null;

        static void InstantiateCoreIfAbsent()
        {
            if (Object.FindObjectsByType<HighScoreTracker>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
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
                    return;
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
