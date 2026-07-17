using UnityEditor;
using UnityEngine;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// <c>GameObject &gt; JamKit &gt; …</c>: instantiates the project's STARTER PREFABS
    /// (<see cref="StarterPrefabLibrary"/>) as linked prefab instances, so scenes stay lists of
    /// prefabs and a starter fix propagates to every scene (PILLARS.md). Composition lives in the
    /// starter assets, not here — customize by making prefab variants of the starters.
    /// </summary>
    public static class JamKitCreateMenu
    {
        const string Menu2D = "GameObject/JamKit/2D/";
        const string Menu3D = "GameObject/JamKit/3D/";
        const string MenuShared = "GameObject/JamKit/";

        [MenuItem(Menu2D + "Player (Platformer)", false, 10)]
        static void Player2DPlatformer(MenuCommand cmd) => Place("Player2D_Platformer", cmd);

        [MenuItem(Menu2D + "Player (Top-Down)", false, 11)]
        static void Player2DTopDown(MenuCommand cmd) => Place("Player2D_TopDown", cmd);

        [MenuItem(Menu2D + "Enemy (Chaser)", false, 12)]
        static void Enemy2D(MenuCommand cmd) => Place("Enemy2D_Chaser", cmd);

        [MenuItem(Menu2D + "Kill Zone", false, 13)]
        static void KillZone2D(MenuCommand cmd) => Place("KillZone2D", cmd);

        [MenuItem(Menu2D + "Follow Camera", false, 14)]
        static void Camera2D(MenuCommand cmd) => Place("FollowCam2D", cmd);

        [MenuItem(Menu3D + "Player", false, 30)]
        static void Player3D(MenuCommand cmd) => Place("Player3D", cmd);

        [MenuItem(Menu3D + "Enemy (Chaser)", false, 31)]
        static void Enemy3D(MenuCommand cmd) => Place("Enemy3D_Chaser", cmd);

        [MenuItem(Menu3D + "Kill Zone", false, 32)]
        static void KillZone3D(MenuCommand cmd) => Place("KillZone3D", cmd);

        [MenuItem(Menu3D + "Follow Camera", false, 33)]
        static void Camera3D(MenuCommand cmd) => Place("FollowCam3D", cmd);

        [MenuItem(MenuShared + "Spawner", false, 50)]
        static void SpawnerPreset(MenuCommand cmd) => Place("Spawner", cmd);

        [MenuItem(MenuShared + "Pickup", false, 51)]
        static void PickupPreset(MenuCommand cmd) => Place("Pickup", cmd);

        static void Place(string starterName, MenuCommand cmd)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StarterPrefabLibrary.PathFor(starterName));
            if (prefab == null)
            {
                if (EditorUtility.DisplayDialog("JamKit",
                        $"Starter prefab '{starterName}' doesn't exist yet.\n\n" +
                        "Run JamKit > New Jam Project to scaffold the starter library (and everything else)?",
                        "Run Wizard", "Cancel"))
                    JamProjectWizard.Run();
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            GameObjectUtility.SetParentAndAlign(go, cmd?.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + starterName);
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}
