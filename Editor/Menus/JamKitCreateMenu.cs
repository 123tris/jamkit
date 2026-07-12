using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// <c>GameObject &gt; JamKit &gt; …</c> presets: composed, pre-wired archetypes so a player
    /// or a chaser enemy is one menu click, not ten component adds. Every preset runs through
    /// <see cref="JamKitAutoAssign"/> at the end, so service references land filled when the
    /// wizard assets exist. Visuals use built-in sprites / primitive meshes — replace the art,
    /// keep the wiring. Feedback is Feel's job: add an MMF_Player and wire Health.OnDamaged →
    /// PlayFeedbacks in the inspector (starters from the wizard come pre-wired).
    /// </summary>
    public static class JamKitCreateMenu
    {
        const string Menu2D = "GameObject/JamKit/2D/";
        const string Menu3D = "GameObject/JamKit/3D/";
        const string MenuShared = "GameObject/JamKit/";

        // ------------------------------------------------------------------ 2D

        [MenuItem(Menu2D + "Player (Platformer)", false, 10)]
        static void Player2DPlatformer(MenuCommand cmd)
        {
            var go = NewSprite("Player", cmd, KnobSprite(), new Color(0.35f, 0.75f, 1f));
            go.tag = "Player";
            var rb = go.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            go.AddComponent<CapsuleCollider2D>();
            go.AddComponent<Mover2D>();
            go.AddComponent<Health>();
            go.AddComponent<HitStop>();
            Finish(go);
        }

        [MenuItem(Menu2D + "Player (Top-Down)", false, 11)]
        static void Player2DTopDown(MenuCommand cmd)
        {
            var go = NewSprite("Player", cmd, KnobSprite(), new Color(0.35f, 0.75f, 1f));
            go.tag = "Player";
            var rb = go.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.gravityScale = 0f;
            go.AddComponent<CircleCollider2D>();
            var mover = go.AddComponent<Mover2D>();
            mover.TopDown = true;
            go.AddComponent<Health>();
            go.AddComponent<HitStop>();
            Finish(go);
        }

        [MenuItem(Menu2D + "Enemy (Chaser)", false, 12)]
        static void Enemy2D(MenuCommand cmd)
        {
            var go = NewSprite("Enemy", cmd, KnobSprite(), new Color(1f, 0.35f, 0.3f));
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            go.AddComponent<CircleCollider2D>();
            go.AddComponent<ChaseMover>();
            var health = go.AddComponent<Health>();
            health.Max = health.Current = 3f;
            health.DestroyOnDeath = true;
            go.AddComponent<Damager>();
            go.AddComponent<SpawnBurst>().Is2D = true;
            Finish(go);
        }

        [MenuItem(Menu2D + "Kill Zone", false, 13)]
        static void KillZone2D(MenuCommand cmd)
        {
            var go = NewGO("KillZone", cmd);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(30f, 1f);
            go.AddComponent<TriggerZone>().Kill = true;
            Finish(go);
        }

        [MenuItem(Menu2D + "Follow Camera", false, 14)]
        static void Camera2D(MenuCommand cmd)
        {
            var go = NewGO("CM FollowCam 2D", cmd);
            go.AddComponent<CinemachineCamera>();
            var follow = go.AddComponent<CinemachineFollow>();
            follow.FollowOffset = new Vector3(0f, 0f, -10f);
            go.AddComponent<FollowCamera>();
            Finish(go);
        }

        // ------------------------------------------------------------------ 3D

        [MenuItem(Menu3D + "Player", false, 30)]
        static void Player3D(MenuCommand cmd)
        {
            var go = NewPrimitive("Player", cmd, PrimitiveType.Capsule, new Color(0.35f, 0.75f, 1f));
            go.tag = "Player";
            go.AddComponent<Rigidbody>();
            go.AddComponent<Mover3D>();
            go.AddComponent<Health>();
            go.AddComponent<HitStop>();
            Finish(go);
        }

        [MenuItem(Menu3D + "Enemy (Chaser)", false, 31)]
        static void Enemy3D(MenuCommand cmd)
        {
            var go = NewPrimitive("Enemy", cmd, PrimitiveType.Capsule, new Color(1f, 0.35f, 0.3f));
            var rb = go.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            go.AddComponent<ChaseMover>();
            var health = go.AddComponent<Health>();
            health.Max = health.Current = 3f;
            health.DestroyOnDeath = true;
            go.AddComponent<Damager>();
            go.AddComponent<SpawnBurst>();
            Finish(go);
        }

        [MenuItem(Menu3D + "Kill Zone", false, 32)]
        static void KillZone3D(MenuCommand cmd)
        {
            var go = NewGO("KillZone", cmd);
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(50f, 1f, 50f);
            go.transform.position += Vector3.down * 10f;
            go.AddComponent<TriggerZone>().Kill = true;
            Finish(go);
        }

        [MenuItem(Menu3D + "Follow Camera", false, 33)]
        static void Camera3D(MenuCommand cmd)
        {
            var go = NewGO("CM FollowCam 3D", cmd);
            go.AddComponent<CinemachineCamera>();
            var follow = go.AddComponent<CinemachineFollow>();
            follow.FollowOffset = new Vector3(0f, 6f, -8f);
            go.AddComponent<CinemachineRotationComposer>();
            go.AddComponent<FollowCamera>();
            Finish(go);
        }

        // ------------------------------------------------------------------ shared

        [MenuItem(MenuShared + "Spawner", false, 50)]
        static void SpawnerPreset(MenuCommand cmd)
        {
            var go = NewGO("Spawner", cmd);
            go.AddComponent<Spawner>();
            Finish(go);
        }

        [MenuItem(MenuShared + "Pickup (2D trigger)", false, 51)]
        static void Pickup2D(MenuCommand cmd)
        {
            var go = NewSprite("Pickup", cmd, KnobSprite(), new Color(1f, 0.9f, 0.3f));
            go.transform.localScale = Vector3.one * 0.5f;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            go.AddComponent<Pickup>().ScoreValue = 1f;
            Finish(go);
        }

        [MenuItem(MenuShared + "Toast", false, 52)]
        static void ToastPreset(MenuCommand cmd)
        {
            var go = NewGO("Toast", cmd);
            go.AddComponent<Toast>();
            Finish(go);
        }

        // ------------------------------------------------------------------ helpers

        static GameObject NewGO(string name, MenuCommand cmd)
        {
            var go = new GameObject(name);
            GameObjectUtility.SetParentAndAlign(go, cmd?.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            return go;
        }

        static GameObject NewSprite(string name, MenuCommand cmd, Sprite sprite, Color color)
        {
            var go = NewGO(name, cmd);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            return go;
        }

        static GameObject NewPrimitive(string name, MenuCommand cmd, PrimitiveType type, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            GameObjectUtility.SetParentAndAlign(go, cmd?.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                // Instance material so presets can be color-coded without editing the default asset.
                var mat = new Material(renderer.sharedMaterial) { name = name + "Mat", color = color };
                renderer.material = mat;
            }
            return go;
        }

        static Sprite KnobSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        static void Finish(GameObject go)
        {
            foreach (var component in go.GetComponentsInChildren<Component>())
                if (component != null && JamKitAutoAssign.IsJamKitType(component.GetType()))
                    JamKitAutoAssign.FillComponent(component, log: false);
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}
