using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// <c>GameObject &gt; JamKit &gt; …</c> presets: fully composed, pre-wired archetypes so a
    /// pong paddle or a survivor enemy is one menu click, not ten component adds. Every preset
    /// runs through <see cref="JamKitAutoAssign"/> at the end, so service references land filled
    /// when the wizard assets exist. Visuals use built-in sprites / primitive meshes — replace
    /// the art, keep the wiring.
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
            go.AddComponent<SpriteFlash>();
            go.AddComponent<PunchScale>();
            AddPlayerHitJuice(go);
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
            go.AddComponent<SpriteFlash>();
            go.AddComponent<PunchScale>();
            AddPlayerHitJuice(go);
            Finish(go);
        }

        [MenuItem(Menu2D + "Player (Grid — Frogger)", false, 12)]
        static void Player2DGrid(MenuCommand cmd)
        {
            var go = NewSprite("GridPlayer", cmd, KnobSprite(), new Color(0.4f, 0.9f, 0.4f));
            go.tag = "Player";
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;
            go.AddComponent<GridMover>();
            var health = go.AddComponent<Health>();
            health.Max = health.Current = 1f;
            go.AddComponent<Respawner>();
            go.AddComponent<SpriteFlash>();
            go.AddComponent<PunchScale>();
            AddPlayerHitJuice(go);
            Finish(go);
        }

        [MenuItem(Menu2D + "Ship (Asteroids)", false, 13)]
        static void Ship2D(MenuCommand cmd)
        {
            var go = NewSprite("Ship", cmd, KnobSprite(), new Color(0.9f, 0.9f, 1f));
            go.tag = "Player";
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            go.AddComponent<CircleCollider2D>();
            go.AddComponent<ThrustMover2D>();
            go.AddComponent<ScreenWrap2D>();
            go.AddComponent<Health>();
            go.AddComponent<Respawner>();
            go.AddComponent<SpriteFlash>();

            // Nose points up (2D ship convention); the muzzle's right axis must match it for
            // ProjectileShooter's 2D fire direction.
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(go.transform, false);
            muzzle.localPosition = new Vector3(0f, 0.5f, 0f);
            muzzle.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var shooter = go.AddComponent<ProjectileShooter>();
            shooter.Is2D = true;
            shooter.Muzzle = muzzle;
            AddPlayerHitJuice(go);
            Finish(go);
        }

        [MenuItem(Menu2D + "Enemy (Chaser)", false, 14)]
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
            go.AddComponent<Damager2D>();
            go.AddComponent<SpriteFlash>();
            go.AddComponent<PunchScale>();
            go.AddComponent<SpawnBurst>().Is2D = true;
            Finish(go);
        }

        [MenuItem(Menu2D + "Ball (Pong-Breakout)", false, 15)]
        static void Ball2D(MenuCommand cmd)
        {
            var go = NewSprite("Ball", cmd, KnobSprite(), Color.white);
            go.transform.localScale = Vector3.one * 0.6f;
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<CircleCollider2D>();
            go.AddComponent<Bouncer2D>();
            var respawn = go.AddComponent<Respawner>();
            respawn.OnSiblingDeath = false; // no Health — wire a goal TriggerZone to Respawn()
            Finish(go);
        }

        [MenuItem(Menu2D + "Paddle", false, 16)]
        static void Paddle2D(MenuCommand cmd)
        {
            var go = NewSprite("Paddle", cmd, BackgroundSprite(), Color.white);
            go.transform.localScale = new Vector3(4f, 0.5f, 1f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<Paddle>(); // english marker — Bouncer2D bends bounces by hit offset
            var mover = go.AddComponent<Mover2D>();
            mover.TopDown = true;
            mover.AxisScale = new Vector2(1f, 0f); // breakout orientation; flip for pong
            mover.MoveSpeed = 10f;
            Finish(go);
        }

        [MenuItem(Menu2D + "Hazard (Patrol — cars, saws)", false, 17)]
        static void Hazard2D(MenuCommand cmd)
        {
            var go = NewSprite("PatrolHazard", cmd, BackgroundSprite(), new Color(1f, 0.6f, 0.2f));
            go.transform.localScale = new Vector3(1.6f, 0.8f, 1f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            go.AddComponent<PatrolMover>().Mode = PatrolMover.EndMode.TeleportToStart;
            go.AddComponent<Damager2D>().Damage = 999f;
            Finish(go);
        }

        [MenuItem(Menu2D + "Kill Zone", false, 18)]
        static void KillZone2D(MenuCommand cmd)
        {
            var go = NewGO("KillZone", cmd);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(30f, 1f);
            go.AddComponent<TriggerZone>().Kill = true;
            Finish(go);
        }

        [MenuItem(Menu2D + "Follow Camera", false, 19)]
        static void Camera2D(MenuCommand cmd)
        {
            var go = NewGO("CM FollowCam 2D", cmd);
            go.AddComponent<CinemachineCamera>();
            var follow = go.AddComponent<CinemachineFollow>();
            follow.FollowOffset = new Vector3(0f, 0f, -10f);
            go.AddComponent<CinemachineFollow2D>();
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
            go.AddComponent<MaterialFlash>();
            go.AddComponent<PunchScale>();
            AddPlayerHitJuice(go);
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
            go.AddComponent<MaterialFlash>();
            go.AddComponent<PunchScale>();
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
            go.AddComponent<CinemachineFollow3D>();
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
            go.AddComponent<Pickup>().ScoreValue = 1;
            Finish(go);
        }

        [MenuItem(MenuShared + "Floating Text Layer", false, 52)]
        static void FloatingTextLayerPreset(MenuCommand cmd)
        {
            var go = NewGO("FloatingTextLayer", cmd);
            go.AddComponent<FloatingTextLayer>();
            Finish(go);
        }

        [MenuItem(MenuShared + "Toast", false, 53)]
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

        /// <summary>
        /// Player damage is the hit that matters most — pre-wire the global-feel receivers
        /// (roadmap decision: players get CameraShake + HitStop, enemies just flash/pop).
        /// Both default to sibling-damage triggers, so they fire only when THIS object is hit.
        /// </summary>
        static void AddPlayerHitJuice(GameObject go)
        {
            go.AddComponent<CameraShake>();
            go.AddComponent<HitStop>();
        }

        static Sprite KnobSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        static Sprite BackgroundSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

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
