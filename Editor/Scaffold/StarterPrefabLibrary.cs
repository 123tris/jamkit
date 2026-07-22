using System;
using Ripple;
using UltEvents;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Editor
{
    /// <summary>
    /// Builds the project's starter prefab ASSETS (<c>Assets/_Project/Prefabs/Starters</c>) —
    /// pre-wired archetypes designers duplicate as prefab VARIANTS and drop into scenes, so
    /// scenes stay lists of prefab instances (PILLARS.md). Run by the wizard; idempotent
    /// (existing starters are never touched, so customizations survive re-running).
    /// When Feel is installed, Health-bearing starters get an MMF_Player with
    /// <c>Health.OnDamaged → PlayFeedbacks()</c> pre-wired as a visible, editable UltEvent call —
    /// the edit-time replacement for the old hidden sibling-Health magic.
    /// </summary>
    public static class StarterPrefabLibrary
    {
        public const string StartersDir = "Assets/_Project/Prefabs/Starters";

        public struct Context
        {
            public InputServiceSO Input;
            public TimeServiceSO Time;
            public PoolServiceSO Pool;
            public FloatVariableSO Score;
        }

        struct Starter
        {
            public string Name;
            public Func<Context, GameObject> Compose;
        }

        // Feel lives in Assembly-CSharp (MMFeedbacks ships no asmdef), so the package can never
        // reference it at compile time — this reflection lookup is the one sanctioned exception.
        static readonly Type FeelPlayerType =
            Type.GetType("MoreMountains.Feedbacks.MMF_Player, Assembly-CSharp");

        static readonly Starter[] Starters =
        {
            new() { Name = "Player2D_Platformer", Compose = ctx =>
            {
                var go = NewSprite("Player2D_Platformer", new Color(0.35f, 0.75f, 1f));
                go.tag = "Player";
                var rb = go.AddComponent<Rigidbody2D>();
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                go.AddComponent<CapsuleCollider2D>();
                go.AddComponent<Mover2D>().InputService = ctx.Input;
                AddPlayerHealth(go, ctx);
                return go;
            }},
            new() { Name = "Player2D_TopDown", Compose = ctx =>
            {
                var go = NewSprite("Player2D_TopDown", new Color(0.35f, 0.75f, 1f));
                go.tag = "Player";
                var rb = go.AddComponent<Rigidbody2D>();
                rb.freezeRotation = true;
                rb.gravityScale = 0f;
                go.AddComponent<CircleCollider2D>();
                var mover = go.AddComponent<Mover2D>();
                mover.TopDown = true;
                mover.InputService = ctx.Input;
                AddPlayerHealth(go, ctx);
                return go;
            }},
            new() { Name = "Player3D", Compose = ctx =>
            {
                var go = NewPrimitive("Player3D", PrimitiveType.Capsule, "PlayerMat", new Color(0.35f, 0.75f, 1f));
                go.tag = "Player";
                go.AddComponent<Rigidbody>();
                go.AddComponent<Mover3D>().InputService = ctx.Input;
                AddPlayerHealth(go, ctx);
                return go;
            }},
            new() { Name = "Enemy2D_Chaser", Compose = ctx =>
            {
                var go = NewSprite("Enemy2D_Chaser", new Color(1f, 0.35f, 0.3f));
                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                go.AddComponent<CircleCollider2D>();
                go.AddComponent<ChaseMover>();
                go.AddComponent<Damager>();
                AddEnemyHealthAndBurst(go, ctx, is2D: true);
                return go;
            }},
            new() { Name = "Enemy3D_Chaser", Compose = ctx =>
            {
                var go = NewPrimitive("Enemy3D_Chaser", PrimitiveType.Capsule, "EnemyMat", new Color(1f, 0.35f, 0.3f));
                var rb = go.AddComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                go.AddComponent<ChaseMover>();
                go.AddComponent<Damager>();
                AddEnemyHealthAndBurst(go, ctx, is2D: false);
                return go;
            }},
            new() { Name = "Pickup", Compose = ctx =>
            {
                var go = NewSprite("Pickup", new Color(1f, 0.9f, 0.3f));
                go.transform.localScale = Vector3.one * 0.5f;
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                var pickup = go.AddComponent<Pickup>();
                pickup.ScoreVariable = ctx.Score;
                pickup.ScoreValue = new FloatReference(1f);
                pickup.PoolService = ctx.Pool;
                return go;
            }},
            new() { Name = "Spawner", Compose = ctx =>
            {
                var go = new GameObject("Spawner");
                go.AddComponent<Spawner>().PoolService = ctx.Pool;
                return go;
            }},
            new() { Name = "KillZone2D", Compose = _ =>
            {
                var go = new GameObject("KillZone2D");
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(30f, 1f);
                go.AddComponent<TriggerZone>().Kill = true;
                return go;
            }},
            new() { Name = "KillZone3D", Compose = _ =>
            {
                var go = new GameObject("KillZone3D");
                var col = go.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(50f, 1f, 50f);
                go.AddComponent<TriggerZone>().Kill = true;
                return go;
            }},
            new() { Name = "FollowCam2D", Compose = _ =>
            {
                var go = new GameObject("FollowCam2D");
                go.AddComponent<CinemachineCamera>();
                var follow = go.AddComponent<CinemachineFollow>();
                follow.FollowOffset = new Vector3(0f, 0f, -10f);
                go.AddComponent<FollowCamera>().OrthographicSize = 5f;
                return go;
            }},
            new() { Name = "FollowCam3D", Compose = _ =>
            {
                var go = new GameObject("FollowCam3D");
                go.AddComponent<CinemachineCamera>();
                var follow = go.AddComponent<CinemachineFollow>();
                follow.FollowOffset = new Vector3(0f, 6f, -8f);
                go.AddComponent<CinemachineRotationComposer>();
                go.AddComponent<FollowCamera>();
                return go;
            }},
        };

        /// <summary>
        /// Create any missing starter prefabs. Call from the wizard with a throwaway scene open
        /// (compose builds scene objects, saves them as prefab assets, then destroys them).
        /// </summary>
        public static void EnsureAll(Context ctx)
        {
            System.IO.Directory.CreateDirectory(StartersDir);
            foreach (var starter in Starters)
            {
                string path = $"{StartersDir}/{starter.Name}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) continue;

                var go = starter.Compose(ctx);
                try
                {
                    PrefabUtility.SaveAsPrefabAsset(go, path);
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        /// <summary>All starter names, for the GameObject menu.</summary>
        public static string PathFor(string starterName) => $"{StartersDir}/{starterName}.prefab";

        // ------------------------------------------------------------------ shared composition

        static void AddPlayerHealth(GameObject go, Context ctx)
        {
            var health = go.AddComponent<Health>();
            var hitStop = go.AddComponent<HitStop>();
            hitStop.TimeService = ctx.Time;
            // Visible wiring, done at edit time: this instance's damage plays this instance's
            // hit-stop (damage-scaled) — inspect/reorder/delete it on the prefab like any UltEvent.
            health.OnDamaged ??= new UltEvent<float>();
            health.OnDamaged.AddPersistentCall((Action<float>)hitStop.Play);
            AddFeelPlayer(go, health);
        }

        static void AddEnemyHealthAndBurst(GameObject go, Context ctx, bool is2D)
        {
            var health = go.AddComponent<Health>();
            health.Max = new FloatReference(3f);
            health.Current = 3f;
            health.DestroyOnDeath = true;
            var burst = go.AddComponent<SpawnBurst>();
            burst.Is2D = is2D;
            burst.PoolService = ctx.Pool;
            health.OnDied ??= new UltEvent();
            health.OnDied.AddPersistentCall((Action)burst.Burst);
            AddFeelPlayer(go, health);
        }

        /// <summary>
        /// Feel present → add an MMF_Player and pre-wire Health.OnDamaged → PlayFeedbacks().
        /// The player starts with an empty feedback stack: designers author the feel, the
        /// trigger is already connected. Feel absent → starters stay Feel-ready but unwired.
        /// </summary>
        static void AddFeelPlayer(GameObject go, Health health)
        {
            if (FeelPlayerType == null) return;
            var player = go.AddComponent(FeelPlayerType);
            var play = FeelPlayerType.GetMethod("PlayFeedbacks", Type.EmptyTypes);
            if (play == null) return;
            var del = Delegate.CreateDelegate(typeof(Action), player, play);
            health.OnDamaged ??= new UltEvent<float>();
            health.OnDamaged.AddPersistentCall(del);
        }

        static GameObject NewSprite(string name, Color color)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            sr.color = color;
            return go;
        }

        static GameObject NewPrimitive(string name, PrimitiveType type, string materialName, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = EnsureMaterial(materialName, color);
            return go;
        }

        /// <summary>Tiny colored material asset (prefabs can't serialize in-memory materials).</summary>
        static Material EnsureMaterial(string name, Color color)
        {
            string path = $"{StartersDir}/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            System.IO.Directory.CreateDirectory($"{StartersDir}/Materials");
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
