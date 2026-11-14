using UnityEditor;
using UnityEngine;

namespace Metz.JamKit.Editor
{
    internal enum Starter
    {
        Player2D_Platformer,
        Player2D_TopDown,
        Enemy2D_Chaser,
        KillZone2D,
        FollowCam2D,
        Player3D,
        Enemy3D_Chaser,
        KillZone3D,
        FollowCam3D,
        Spawner,
        Pickup,
    }

    public static class JamKitCreateMenu
    {
        const string Menu2D = "GameObject/JamKit/2D/";
        const string Menu3D = "GameObject/JamKit/3D/";
        const string MenuShared = "GameObject/JamKit/";

        [MenuItem(Menu2D + "Player (Platformer)", false, 10)]
        static void Player2DPlatformer(MenuCommand cmd) => Place(Starter.Player2D_Platformer, cmd);

        [MenuItem(Menu2D + "Player (Top-Down)", false, 11)]
        static void Player2DTopDown(MenuCommand cmd) => Place(Starter.Player2D_TopDown, cmd);

        [MenuItem(Menu2D + "Enemy (Chaser)", false, 12)]
        static void Enemy2D(MenuCommand cmd) => Place(Starter.Enemy2D_Chaser, cmd);

        [MenuItem(Menu2D + "Kill Zone", false, 13)]
        static void KillZone2D(MenuCommand cmd) => Place(Starter.KillZone2D, cmd);

        [MenuItem(Menu2D + "Follow Camera", false, 14)]
        static void Camera2D(MenuCommand cmd) => Place(Starter.FollowCam2D, cmd);

        [MenuItem(Menu3D + "Player", false, 30)]
        static void Player3D(MenuCommand cmd) => Place(Starter.Player3D, cmd);

        [MenuItem(Menu3D + "Enemy (Chaser)", false, 31)]
        static void Enemy3D(MenuCommand cmd) => Place(Starter.Enemy3D_Chaser, cmd);

        [MenuItem(Menu3D + "Kill Zone", false, 32)]
        static void KillZone3D(MenuCommand cmd) => Place(Starter.KillZone3D, cmd);

        [MenuItem(Menu3D + "Follow Camera", false, 33)]
        static void Camera3D(MenuCommand cmd) => Place(Starter.FollowCam3D, cmd);

        [MenuItem(MenuShared + "Spawner", false, 50)]
        static void SpawnerStarter(MenuCommand cmd) => Place(Starter.Spawner, cmd);

        [MenuItem(MenuShared + "Pickup", false, 51)]
        static void PickupStarter(MenuCommand cmd) => Place(Starter.Pickup, cmd);

        static void Place(Starter starter, MenuCommand cmd)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectTemplate.StarterPath(starter.ToString()));
            if (prefab == null)
            {
                if (EditorUtility.DisplayDialog("JamKit",
                        $"Starter '{starter}' isn't in this project.\n\n" +
                        "Run JamKit > New Jam Project to scaffold it, along with anything else that is missing?",
                        "Run Wizard", "Cancel"))
                    JamProjectWizard.Run();
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            GameObjectUtility.SetParentAndAlign(go, cmd?.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + starter);
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}
