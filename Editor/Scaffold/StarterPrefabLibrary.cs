using System;
using System.IO;
using Ripple;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Metz.JamKit.Editor
{
    public static class StarterPrefabLibrary
    {
        public const string StartersDir = "Assets/_Project/Prefabs/Starters";

        static readonly Color PlayerColor = new(0.35f, 0.75f, 1f);
        static readonly Color EnemyColor = new(1f, 0.35f, 0.3f);
        static readonly Color PickupColor = new(1f, 0.9f, 0.3f);

        static readonly Type FeelPlayerType = Type.GetType("MoreMountains.Feedbacks.MMF_Player, Assembly-CSharp");

        public struct Context
        {
            public TimeServiceSO Time;
            public FloatVariableSO Score;
            public GameObjectListSO Players;
        }

        static readonly (string name, Func<Context, GameObject> compose)[] Starters =
        {
            ("Player2D_Platformer", context =>
            {
                GameObject player = NewSprite("Player2D_Platformer", PlayerColor);
                var body = player.AddComponent<Rigidbody2D>();
                body.freezeRotation = true;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                player.AddComponent<CapsuleCollider2D>();
                AddMover2D(player, topDown: false);
                MakePlayer(player, context);
                return player;
            }),
            ("Player2D_TopDown", context =>
            {
                GameObject player = NewSprite("Player2D_TopDown", PlayerColor);
                var body = player.AddComponent<Rigidbody2D>();
                body.freezeRotation = true;
                body.gravityScale = 0f;
                player.AddComponent<CircleCollider2D>();
                AddMover2D(player, topDown: true);
                MakePlayer(player, context);
                return player;
            }),
            ("Player3D", context =>
            {
                GameObject player = NewPrimitive("Player3D", "PlayerMat", PlayerColor);
                player.AddComponent<Rigidbody>();
                var mover = player.AddComponent<Mover3D>();
                mover.Move = InputReferences.Find("Move");
                mover.Jump = InputReferences.Find("Jump");
                MakePlayer(player, context);
                return player;
            }),
            ("Enemy2D_Chaser", context =>
            {
                GameObject enemy = NewSprite("Enemy2D_Chaser", EnemyColor);
                var body = enemy.AddComponent<Rigidbody2D>();
                body.gravityScale = 0f;
                body.freezeRotation = true;
                enemy.AddComponent<CircleCollider2D>();
                MakeEnemy(enemy, context, is2D: true);
                return enemy;
            }),
            ("Enemy3D_Chaser", context =>
            {
                GameObject enemy = NewPrimitive("Enemy3D_Chaser", "EnemyMat", EnemyColor);
                enemy.AddComponent<Rigidbody>().constraints =
                    RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                MakeEnemy(enemy, context, is2D: false);
                return enemy;
            }),
            ("Pickup", context =>
            {
                GameObject pickup = NewSprite("Pickup", PickupColor);
                pickup.transform.localScale = Vector3.one * 0.5f;
                pickup.AddComponent<CircleCollider2D>().isTrigger = true;
                var zone = pickup.AddComponent<TriggerZone>();
                zone.RequiredTag = "Player";
                zone.OneShot = true;
                zone.DestroySelf = true;
                UnityEventTools.AddFloatPersistentListener(zone.OnEntered.Response, context.Score.Add, 1f);
                return pickup;
            }),
            ("Spawner", _ =>
            {
                var spawner = new GameObject("Spawner");
                spawner.AddComponent<Spawner>();
                return spawner;
            }),
            ("KillZone2D", _ =>
            {
                var zone = new GameObject("KillZone2D");
                var area = zone.AddComponent<BoxCollider2D>();
                area.isTrigger = true;
                area.size = new Vector2(30f, 1f);
                zone.AddComponent<Damager>().Kill = true;
                return zone;
            }),
            ("KillZone3D", _ =>
            {
                var zone = new GameObject("KillZone3D");
                var area = zone.AddComponent<BoxCollider>();
                area.isTrigger = true;
                area.size = new Vector3(50f, 1f, 50f);
                zone.AddComponent<Damager>().Kill = true;
                return zone;
            }),
            ("FollowCam2D", _ =>
            {
                GameObject rig = NewFollowCamera("FollowCam2D", new Vector3(0f, 0f, -10f));
                var camera = rig.GetComponent<CinemachineCamera>();
                LensSettings lens = camera.Lens;
                lens.OrthographicSize = 5f;
                camera.Lens = lens;
                return rig;
            }),
            ("FollowCam3D", _ =>
            {
                GameObject rig = NewFollowCamera("FollowCam3D", new Vector3(0f, 6f, -8f));
                rig.AddComponent<CinemachineRotationComposer>();
                return rig;
            }),
        };

        public static void EnsureAll(Context context)
        {
            Directory.CreateDirectory(StartersDir);
            foreach ((string name, Func<Context, GameObject> compose) in Starters)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PathFor(name)) != null) continue;
                GameObject starter = compose(context);
                PrefabUtility.SaveAsPrefabAsset(starter, PathFor(name));
                Object.DestroyImmediate(starter);
            }
        }

        public static string PathFor(string starterName) => $"{StartersDir}/{starterName}.prefab";

        static void AddMover2D(GameObject player, bool topDown)
        {
            var mover = player.AddComponent<Mover2D>();
            mover.TopDown = topDown;
            mover.Move = InputReferences.Find("Move");
            mover.Jump = InputReferences.Find("Jump");
        }

        static void MakePlayer(GameObject player, Context context)
        {
            player.tag = "Player";
            SetReference(player.AddComponent<RuntimeSetMember>(), "set", context.Players);

            var health = player.AddComponent<Health>();
            var hitStop = player.AddComponent<HitStop>();
            hitStop.TimeService = context.Time;
            UnityEventTools.AddPersistentListener(health.OnDamaged.Response, hitStop.Play);
            AddJuice(player, health);
        }

        static void MakeEnemy(GameObject enemy, Context context, bool is2D)
        {
            enemy.AddComponent<ChaseMover>().Targets = context.Players;
            enemy.AddComponent<Damager>();

            var health = enemy.AddComponent<Health>();
            health.Max = new FloatReference(3f);
            health.Current = 3f;
            health.DestroyOnDeath = true;

            var burst = enemy.AddComponent<SpawnBurst>();
            burst.Is2D = is2D;
            UnityEventTools.AddVoidPersistentListener(health.OnDied.Response, burst.Burst);
            AddJuice(enemy, health);
        }

        static void AddJuice(GameObject target, Health health)
        {
            var punch = target.AddComponent<PunchScale>();
            UnityEventTools.AddVoidPersistentListener(health.OnDamaged.Response, punch.Play);

            if (FeelPlayerType == null) return;
            Component feel = target.AddComponent(FeelPlayerType);
            var play = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), feel,
                FeelPlayerType.GetMethod("PlayFeedbacks", Type.EmptyTypes));
            UnityEventTools.AddVoidPersistentListener(health.OnDamaged.Response, play);
        }

        internal static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject NewFollowCamera(string name, Vector3 offset)
        {
            var rig = new GameObject(name);
            rig.AddComponent<CinemachineCamera>();
            rig.AddComponent<CinemachineFollow>().FollowOffset = offset;
            rig.AddComponent<CinemachineImpulseListener>();
            return rig;
        }

        static GameObject NewSprite(string name, Color color)
        {
            var sprite = new GameObject(name);
            var renderer = sprite.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            renderer.color = color;
            return sprite;
        }

        static GameObject NewPrimitive(string name, string materialName, Color color)
        {
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            primitive.name = name;
            primitive.GetComponent<Renderer>().sharedMaterial = EnsureMaterial(materialName, color);
            return primitive;
        }

        static Material EnsureMaterial(string name, Color color)
        {
            string path = $"{StartersDir}/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            Directory.CreateDirectory($"{StartersDir}/Materials");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
