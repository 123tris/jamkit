// Sample-content authoring for JamKit — NOT part of the package assemblies (lives in Tools~).
// Usage: rename the package's Samples~ folder to Samples, copy this file into the dev project's
// Assets/Editor/, then run Unity in batch mode:
//   Unity.exe -batchmode -projectPath "<dev project>" -executeMethod Metz.JamKitDev.JamKitSampleAuthoring.AuthorAll -quit -logFile <log>
// Requires Feel in the project (sample 03). Rename Samples back to Samples~ afterwards.
// Idempotent: a sample whose scene already exists is skipped, so re-runs only build what's missing.
using System;
using MoreMountains.Feedbacks;
using Ripple;
using UltEvents;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Metz.JamKit;
using Metz.JamKit.Samples;
using Object = UnityEngine.Object;

namespace Metz.JamKitDev
{
    public static class JamKitSampleAuthoring
    {
        const string Root = "Packages/com.metz.jamkit/Samples";

        public static void AuthorAll()
        {
            try
            {
                Author00HourZero();
                Author01Platformer();
                Author02Survivor();
                Author03FeelShowcase();
                Author04Arcade();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[JamKitDev] Sample authoring complete.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[JamKitDev] Sample authoring FAILED: {e}");
                EditorApplication.Exit(1);
            }
        }

        // ------------------------------------------------------------------ 00 Hour Zero

        static void Author00HourZero()
        {
            const string dir = Root + "/00 Hour Zero";
            if (SceneExists(dir, "00 Hour Zero")) return;
            NewScene(orthographic: false, cameraPos: new Vector3(0f, 4f, -12f));

            var caught = Asset<VoidEventSO>($"{dir}/Data/CrateCaught.asset");
            var score = FloatVar($"{dir}/Data/DemoScore.asset", 0f, 999999f, 0f);

            // Crate: pooled physics debris with a lifetime — Spawner + Pool + AutoDespawn in one glance.
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "Crate";
            crate.transform.localScale = Vector3.one * 0.6f;
            crate.AddComponent<Rigidbody>();
            crate.AddComponent<AutoDespawn>().Seconds = 6f;
            var cratePrefab = SavePrefab(crate, dir, "Crate");

            // CrateSpawner: rains crates from above (PoolService wired at sample setup).
            var spawner = new GameObject("CrateSpawner");
            spawner.transform.position = new Vector3(0f, 6f, 0f);
            var sp = spawner.AddComponent<Spawner>();
            sp.Prefab = cratePrefab;
            sp.Interval = 0.8f;
            sp.Jitter = new Vector2(2.5f, 2.5f);
            SaveInScene(spawner, dir, "CrateSpawner");

            // CatchZone: TriggerZone as score gate — swallows crates, bumps the demo score,
            // broadcasts the sample event. Zero scripts.
            var zone = new GameObject("CatchZone");
            zone.transform.position = new Vector3(0f, -1.5f, 0f);
            var col = zone.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(12f, 1f, 12f);
            var tz = zone.AddComponent<TriggerZone>();
            tz.RemoveEnterer = true;
            tz.ScoreVariable = score;
            tz.ScoreValue = 1f;
            tz.BroadcastEntered = caught;
            SaveInScene(zone, dir, "CatchZone");

            // Toast narrates the event — the no-code Ripple → UI path.
            var toast = new GameObject("SampleToast");
            var t = toast.AddComponent<Toast>();
            t.Messages = new[] { new Toast.VoidMessage { Event = caught, Message = "+1!" } };
            t.Duration = 0.7f;
            t.FontSize = 34;
            SaveInScene(toast, dir, "SampleToast");

            SaveSceneAs(dir, "00 Hour Zero");
        }

        // ------------------------------------------------------------------ 01 Platformer

        static void Author01Platformer()
        {
            const string dir = Root + "/01 Platformer";
            if (SceneExists(dir, "01 Platformer")) return;
            NewScene(orthographic: true, cameraPos: new Vector3(0f, 1f, -10f));

            var goalReached = Asset<VoidEventSO>($"{dir}/Data/GoalReached.asset");

            // Player: the visible-wiring showcase — death → respawn → refill, all UltEvents.
            var player = Sprite("Player", new Color(0.35f, 0.75f, 1f));
            player.tag = "Player";
            var rb = player.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.AddComponent<CapsuleCollider2D>();
            player.AddComponent<Mover2D>();
            var health = player.AddComponent<Health>();
            health.Max = health.Current = 3f;
            var hitStop = player.AddComponent<HitStop>();
            var respawner = player.AddComponent<Respawner>();
            Call(ref health.OnDamaged, (Action<float>)hitStop.Play);
            Call(ref health.OnDied, respawner.RespawnAfterDelay);
            Call(ref respawner.OnRespawned, health.ResetFull);
            var playerGo = SaveInScene(player, dir, "Player");

            // Ground + platforms.
            var platform = Sprite("Platform", new Color(0.5f, 0.55f, 0.65f), background: true);
            platform.transform.localScale = new Vector3(6f, 0.5f, 1f);
            platform.AddComponent<BoxCollider2D>();
            var platformPrefab = SavePrefab(platform, dir, "Platform");
            Place(platformPrefab, new Vector3(0f, -2f, 0f));
            Place(platformPrefab, new Vector3(7f, -0.5f, 0f));
            Place(platformPrefab, new Vector3(14f, 1f, 0f));

            // Patrol hazard: frogger-car energy — 1 damage per touch.
            var hazard = Sprite("PatrolHazard", new Color(1f, 0.6f, 0.2f), background: true);
            hazard.transform.localScale = new Vector3(1.4f, 0.7f, 1f);
            var hrb = hazard.AddComponent<Rigidbody2D>();
            hrb.bodyType = RigidbodyType2D.Kinematic;
            var hcol = hazard.AddComponent<BoxCollider2D>();
            hcol.isTrigger = true;
            var patrol = hazard.AddComponent<PatrolMover>();
            patrol.PathOffsets = new[] { new Vector3(4f, 0f, 0f) };
            patrol.Speed = 3f;
            var dmg = hazard.AddComponent<Damager>();
            dmg.OncePerTarget = true;
            var hazardPrefab = SavePrefab(hazard, dir, "PatrolHazard");
            Place(hazardPrefab, new Vector3(5f, 0.2f, 0f));

            // Kill pit below everything.
            var pit = new GameObject("KillPit");
            pit.transform.position = new Vector3(6f, -6f, 0f);
            var pcol = pit.AddComponent<BoxCollider2D>();
            pcol.isTrigger = true;
            pcol.size = new Vector2(60f, 1f);
            pit.AddComponent<TriggerZone>().Kill = true;
            SaveInScene(pit, dir, "KillPit");

            // Goal: one-shot zone → Toast, filtered to the player.
            var goal = Sprite("Goal", new Color(0.4f, 0.95f, 0.5f));
            goal.transform.position = new Vector3(14f, 2.2f, 0f);
            var gcol = goal.AddComponent<CircleCollider2D>();
            gcol.isTrigger = true;
            var gz = goal.AddComponent<TriggerZone>();
            gz.OneShot = true;
            gz.RequiredTag = "Player";
            gz.BroadcastEntered = goalReached;
            SaveInScene(goal, dir, "Goal");

            var toast = new GameObject("SampleToast");
            var t = toast.AddComponent<Toast>();
            t.Messages = new[] { new Toast.VoidMessage { Event = goalReached, Message = "You made it!" } };
            SaveInScene(toast, dir, "SampleToast");

            // Follow camera prefab; target is a scene-level instance override.
            var cam = new GameObject("FollowCam");
            cam.AddComponent<CinemachineCamera>();
            cam.AddComponent<CinemachineFollow>().FollowOffset = new Vector3(0f, 1f, -10f);
            var fc = cam.AddComponent<FollowCamera>();
            fc.OrthographicSize = 6f;
            var camGo = SaveInScene(cam, dir, "FollowCam");
            camGo.GetComponent<FollowCamera>().Target = playerGo.transform;

            SaveSceneAs(dir, "01 Platformer");
        }

        // ------------------------------------------------------------------ 02 Survivor

        static void Author02Survivor()
        {
            const string dir = Root + "/02 Survivor";
            if (SceneExists(dir, "02 Survivor")) return;
            NewScene(orthographic: false, cameraPos: new Vector3(0f, 14f, -12f), cameraEuler: new Vector3(45f, 0f, 0f));

            var enemyDied = Asset<VoidEventSO>($"{dir}/Data/EnemyDied.asset");

            // Debris shard for enemy death bursts.
            var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "Shard";
            shard.transform.localScale = Vector3.one * 0.25f;
            shard.AddComponent<Rigidbody>();
            shard.AddComponent<AutoDespawn>().Seconds = 2f;
            var shardPrefab = SavePrefab(shard, dir, "Shard");

            // Enemy: chases, hurts on contact, bursts + broadcasts on death.
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "Enemy";
            var erb = enemy.AddComponent<Rigidbody>();
            erb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            enemy.AddComponent<ChaseMover>();
            enemy.AddComponent<Damager>();
            var eh = enemy.AddComponent<Health>();
            eh.Max = eh.Current = 3f;
            eh.DestroyOnDeath = true;
            eh.BroadcastDied = enemyDied;
            var burst = enemy.AddComponent<SpawnBurst>();
            burst.Prefab = shardPrefab;
            burst.Count = 4;
            burst.LaunchSpeed = 4f;
            Call(ref eh.OnDied, burst.Burst);
            var enemyPrefab = SavePrefab(enemy, dir, "Enemy");

            // Projectile: pooled, damaging, self-cleaning.
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Projectile";
            projectile.transform.localScale = Vector3.one * 0.3f;
            var prb = projectile.AddComponent<Rigidbody>();
            prb.useGravity = false;
            var pdmg = projectile.AddComponent<Damager>();
            pdmg.DestroyOnHit = true;
            projectile.AddComponent<AutoDespawn>().Seconds = 3f;
            var projectilePrefab = SavePrefab(projectile, dir, "Projectile");

            // Player: mover + aimer + shooter; death ends the run (wired below on the arena).
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1.1f, 0f);
            player.AddComponent<Rigidbody>();
            player.AddComponent<Mover3D>();
            var ph = player.AddComponent<Health>();
            ph.Max = ph.Current = 5f;
            var phs = player.AddComponent<HitStop>();
            Call(ref ph.OnDamaged, (Action<float>)phs.Play);
            var pivot = new GameObject("AimPivot").transform;
            pivot.SetParent(player.transform, false);
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(pivot, false);
            muzzle.localPosition = new Vector3(0f, 0f, 0.8f);
            var aimer = player.AddComponent<Aimer>();
            aimer.Plane = Aimer.AimPlane.XZ;
            aimer.Pivot = pivot;
            var shooter = player.AddComponent<ProjectileShooter>();
            shooter.ProjectilePrefab = projectilePrefab;
            shooter.Muzzle = muzzle;
            var playerGo = SaveInScene(player, dir, "Player");

            // Arena: ground + waves + round timer + the glue script, one prefab.
            var arena = new GameObject("SurvivorArena");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(arena.transform, false);
            ground.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
            var spawnPoint = new GameObject("SpawnPoint").transform;
            spawnPoint.SetParent(arena.transform, false);
            spawnPoint.position = new Vector3(8f, 0.5f, 8f);
            var waves = arena.AddComponent<WaveSpawner>();
            waves.SpawnPoint = spawnPoint;
            waves.Jitter = new Vector2(3f, 3f);
            waves.Waves.Add(new WaveSpawner.Wave { Prefab = enemyPrefab, Count = 4, Duration = 6f, DelayBefore = 2f });
            waves.Waves.Add(new WaveSpawner.Wave { Prefab = enemyPrefab, Count = 7, Duration = 8f, DelayBefore = 4f });
            waves.Waves.Add(new WaveSpawner.Wave { Prefab = enemyPrefab, Count = 10, Duration = 10f, DelayBefore = 4f });
            var timer = arena.AddComponent<GameTimer>();
            timer.Duration = new FloatReference(60f);
            var loop = arena.AddComponent<SurvivorLoop>();
            loop.EnemyDied = enemyDied;
            Call(ref timer.Completed, loop.OnTimerDone);
            var arenaGo = SaveInScene(arena, dir, "Prefabs", "SurvivorArena");

            // Player death ends the run too — scene-level wiring across instances.
            var sceneHealth = playerGo.GetComponent<Health>();
            Call(ref sceneHealth.OnDied, arenaGo.GetComponent<SurvivorLoop>().OnTimerDone);

            SaveSceneAs(dir, "02 Survivor");
        }

        // ------------------------------------------------------------------ 03 Feel Showcase

        static void Author03FeelShowcase()
        {
            const string dir = Root + "/03 Feel Showcase";
            if (SceneExists(dir, "03 Feel Showcase")) return;
            NewScene(orthographic: false, cameraPos: new Vector3(0f, 2f, -8f));

            // FeelTarget: Health → FeelPlayer (intensity) → MMF_Player stack, plus the
            // stack-safe hit-stop feedback. Click Health's Damage button to fire the whole chain.
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "FeelTarget";
            var health = target.AddComponent<Health>();
            health.Max = health.Current = 5f;
            var hitStop = target.AddComponent<HitStop>();

            var mmf = target.AddComponent<MMF_Player>();
            var scale = (MMF_Scale)mmf.AddFeedback(typeof(MMF_Scale));
            scale.Label = "Punch Scale";
            scale.AnimateScaleTarget = target.transform;
            scale.AnimateScaleDuration = 0.15f;
            var flicker = (MMF_Flicker)mmf.AddFeedback(typeof(MMF_Flicker));
            flicker.Label = "Damage Flicker";
            flicker.BoundRenderer = target.GetComponent<Renderer>();
            flicker.FlickerDuration = 0.15f;
            var stop = (MMF_JamKitHitStop)mmf.AddFeedback(typeof(MMF_JamKitHitStop));
            stop.Label = "JamKit Hit Stop";
            stop.HitStop = hitStop;

            var feelPlayer = target.AddComponent<FeelPlayer>();
            feelPlayer.Player = mmf;
            Call(ref health.OnDamaged, (Action<float>)feelPlayer.Play);

            var targetPrefab = SavePrefab(target, dir, "FeelTarget");
            Place(targetPrefab, new Vector3(-2.5f, 0.5f, 0f));
            Place(targetPrefab, new Vector3(0f, 0.5f, 0f));
            Place(targetPrefab, new Vector3(2.5f, 0.5f, 0f));

            SaveSceneAs(dir, "03 Feel Showcase");
        }

        // ------------------------------------------------------------------ 04 Arcade

        static void Author04Arcade()
        {
            const string dir = Root + "/04 Arcade";
            if (SceneExists(dir, "04 Arcade")) return;
            NewScene(orthographic: true, cameraPos: new Vector3(0f, 0f, -10f));

            var goalScored = Asset<VoidEventSO>($"{dir}/Data/GoalScored.asset");

            // Court walls.
            var wall = Sprite("Wall", new Color(0.5f, 0.55f, 0.65f), background: true);
            wall.transform.localScale = new Vector3(18f, 0.5f, 1f);
            wall.AddComponent<BoxCollider2D>();
            var wallPrefab = SavePrefab(wall, dir, "Wall");
            Place(wallPrefab, new Vector3(0f, 5f, 0f));
            Place(wallPrefab, new Vector3(0f, -5f, 0f));

            // Paddle: english marker + vertical-only mover (both paddles share the Move action).
            var paddle = Sprite("Paddle", Color.white, background: true);
            paddle.transform.localScale = new Vector3(0.5f, 3f, 1f);
            var prb = paddle.AddComponent<Rigidbody2D>();
            prb.bodyType = RigidbodyType2D.Kinematic;
            paddle.AddComponent<BoxCollider2D>();
            paddle.AddComponent<Paddle>();
            var pm = paddle.AddComponent<Mover2D>();
            pm.TopDown = true;
            pm.AxisScale = new Vector2(0f, 1f);
            pm.MoveSpeed = 9f;
            var paddlePrefab = SavePrefab(paddle, dir, "Paddle");
            Place(paddlePrefab, new Vector3(-8f, 0f, 0f));

            // Breakout wall on the right: bricks shatter via SpawnBurst.
            var shard = Sprite("BrickShard", new Color(0.9f, 0.5f, 0.3f), background: true);
            shard.transform.localScale = new Vector3(0.3f, 0.15f, 1f);
            var srb = shard.AddComponent<Rigidbody2D>();
            srb.gravityScale = 1f;
            shard.AddComponent<AutoDespawn>().Seconds = 1.5f;
            var shardPrefab = SavePrefab(shard, dir, "BrickShard");

            var brick = Sprite("Brick", new Color(0.9f, 0.5f, 0.3f), background: true);
            brick.transform.localScale = new Vector3(0.6f, 1.2f, 1f);
            brick.AddComponent<BoxCollider2D>();
            var bh = brick.AddComponent<Health>();
            bh.Max = bh.Current = 1f;
            bh.DestroyOnDeath = true;
            var bburst = brick.AddComponent<SpawnBurst>();
            bburst.Prefab = shardPrefab;
            bburst.Count = 3;
            bburst.Is2D = true;
            bburst.LaunchSpeed = 3f;
            Call(ref bh.OnDied, bburst.Burst);
            var brickPrefab = SavePrefab(brick, dir, "Brick");
            for (int col = 0; col < 3; col++)
                for (int row = 0; row < 6; row++)
                    Place(brickPrefab, new Vector3(7.5f + col * 0.7f, -3.2f + row * 1.3f, 0f));

            // Ball: bounces forever, gains english off paddles, 1 damage to bricks on touch.
            var ball = Sprite("Ball", Color.white);
            ball.transform.localScale = Vector3.one * 0.55f;
            var brb = ball.AddComponent<Rigidbody2D>();
            brb.gravityScale = 0f;
            ball.AddComponent<CircleCollider2D>();
            var bouncer = ball.AddComponent<Bouncer2D>();
            var bdmg = ball.AddComponent<Damager>();
            bdmg.OncePerTarget = false;
            var ballResp = ball.AddComponent<Respawner>();
            ballResp.Delay = 0.8f;
            // Respawn zeroes velocity, so the serve is wired right back onto the launch.
            Call(ref ballResp.OnRespawned, bouncer.Launch);
            var ballGo = SaveInScene(ball, dir, "Ball");

            // Goal behind the paddle: swallow nothing, just toast + reset the ball (scene wiring).
            var goal = new GameObject("Goal");
            goal.transform.position = new Vector3(-10.5f, 0f, 0f);
            var gcol = goal.AddComponent<BoxCollider2D>();
            gcol.isTrigger = true;
            gcol.size = new Vector2(1f, 12f);
            var gz = goal.AddComponent<TriggerZone>();
            gz.BroadcastEntered = goalScored;
            var goalGo = SaveInScene(goal, dir, "Goal");
            // Scene-level wiring across two prefab instances (an instance override on the goal):
            // any goal entry resets the ball. The enterer argument is simply ignored.
            var gzInstance = goalGo.GetComponent<TriggerZone>();
            gzInstance.OnEntered ??= new UltEvent<GameObject>();
            AddCallIgnoringArg(gzInstance.OnEntered, ballGo.GetComponent<Respawner>().Respawn);

            var toast = new GameObject("SampleToast");
            var t = toast.AddComponent<Toast>();
            t.Messages = new[] { new Toast.VoidMessage { Event = goalScored, Message = "Goal!" } };
            SaveInScene(toast, dir, "SampleToast");

            SaveSceneAs(dir, "04 Arcade");
        }

        // ------------------------------------------------------------------ helpers

        static void NewScene(bool orthographic, Vector3 cameraPos, Vector3 cameraEuler = default)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = orthographic;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.09f);
            camGo.transform.position = cameraPos;
            camGo.transform.rotation = Quaternion.Euler(cameraEuler);
            camGo.AddComponent<CinemachineBrain>();
            camGo.AddComponent<CinemachineExternalImpulseListener>();
            camGo.AddComponent<AudioListener>();

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        static bool SceneExists(string dir, string name)
        {
            bool exists = System.IO.File.Exists($"{dir}/{name}.unity");
            if (exists) Debug.Log($"[JamKitDev] {name} already authored — skipped.");
            return exists;
        }

        static void SaveSceneAs(string dir, string name)
        {
            System.IO.Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene, $"{dir}/{name}.unity");
            Debug.Log($"[JamKitDev] Authored {name}.");
        }

        /// <summary>Save as prefab asset and destroy the scene temp (for spawn-only prefabs).</summary>
        static GameObject SavePrefab(GameObject go, string dir, string name)
        {
            string folder = $"{dir}/Prefabs";
            System.IO.Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{folder}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>Save as prefab asset and keep a linked instance in the scene.</summary>
        static GameObject SaveInScene(GameObject go, string dir, string name)
            => SaveInScene(go, dir, "Prefabs", name);

        static GameObject SaveInScene(GameObject go, string dir, string subfolder, string name)
        {
            string folder = $"{dir}/{subfolder}";
            System.IO.Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            // SaveAsPrefabAssetAndConnect RETURNS THE ASSET ROOT — return the connected scene
            // instance instead, so cross-object wiring targets instances (asset→asset component
            // references throw ArgumentException).
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, $"{folder}/{name}.prefab", InteractionMode.AutomatedAction);
            return go;
        }

        static void Place(GameObject prefab, Vector3 position)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = position;
        }

        static GameObject Sprite(string name, Color color, bool background = false)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(background ? "UI/Skin/Background.psd" : "UI/Skin/Knob.psd");
            sr.color = color;
            return go;
        }

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            AssetDatabase.Refresh();
            var inst = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(inst, path);
            return inst;
        }

        static FloatVariableSO FloatVar(string path, float min, float max, float initial)
        {
            // min/max are VariableReference structs on this Ripple branch — leave the defaults
            // (no clamping needed for demo variables); only the initial value is set.
            var v = Asset<FloatVariableSO>(path);
            var so = new SerializedObject(v);
            var prop = so.FindProperty("_initialValue");
            if (prop != null && prop.propertyType == SerializedPropertyType.Float) prop.floatValue = initial;
            so.ApplyModifiedPropertiesWithoutUndo();
            return v;
        }

        // ---- UltEvent persistent-call wiring (visible in the inspector, editable, reorderable) ----

        static void Call(ref UltEvent evt, Action method)
        {
            evt ??= new UltEvent();
            evt.AddPersistentCall(method);
        }

        static void Call<T>(ref UltEvent<T> evt, Action<T> method)
        {
            evt ??= new UltEvent<T>();
            evt.AddPersistentCall(method);
        }

        static void Call<T>(ref UltEvent<T> evt, Action method)
        {
            evt ??= new UltEvent<T>();
            evt.AddPersistentCall(method);
        }

        /// <summary>Wire a parameterless method to a parameterized event (the arg is ignored).</summary>
        static void AddCallIgnoringArg<T>(UltEvent<T> evt, Action method)
            => evt.AddPersistentCall(method);
    }
}
