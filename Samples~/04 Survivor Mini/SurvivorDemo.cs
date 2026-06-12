using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit.Samples
{
    /// <summary>
    /// Sample 04 — Survivor Mini. A complete (tiny) loop built entirely from JamKit pieces, using
    /// only primitive meshes so it needs no art:
    ///   • A <see cref="Mover3D"/> player on a tilted top-down camera.
    ///   • A <see cref="Spawner"/> that pools collectibles (sphere + <see cref="Pickup"/> + <see cref="AutoDespawn"/>).
    ///   • <see cref="ScoreServiceSO"/> for points (awarded by the Pickup) and <see cref="TimerServiceSO"/> for the countdown.
    ///   • A code-built HUD. (For a no-code HUD, add <see cref="LabelBinding"/> instead — see the README.)
    /// When the timer runs out it loads the GameOver scene. Drop this on an empty GameObject in a
    /// scene that has a JamKitCore (the wizard's Game scene does) and assign the service SOs.
    /// </summary>
    public sealed class SurvivorDemo : MonoBehaviour
    {
        [Header("Services (assign the wizard's SOs)")]
        public InputServiceSO InputService;
        public PoolServiceSO PoolService;
        public ScoreServiceSO ScoreService;
        public TimerServiceSO TimerService;
        public SceneServiceSO SceneService;

        [Header("Run")]
        public float RunSeconds = 30f;
        public string GameOverScene = "GameOver";

        [Header("Spawning")]
        public float SpawnInterval = 0.6f;
        public int MaxAlive = 25;
        public float ArenaRadius = 20f;

        const int PlayerLayer = 0;       // Default — what pickups collect
        const int PropLayer = 2;         // Ignore Raycast — floor + collectibles (never collect each other)

        Label _scoreLabel, _timeLabel;
        bool _ending;
        bool _started;

        void Awake()
        {
            BuildCamera();
            BuildFloor();
            BuildPlayer();
            var prefab = BuildCollectiblePrefab();
            BuildSpawner(prefab);
            BuildHud();
        }

        void Start()
        {
            if (ScoreService != null) ScoreService.ResetScore();
            if (TimerService != null)
            {
                TimerService.CountMode = TimerServiceSO.Mode.CountDown;
                TimerService.StartTimer(RunSeconds);
                _started = true;
            }
        }

        void Update()
        {
            if (_scoreLabel != null && ScoreService != null) _scoreLabel.text = $"Score: {ScoreService.Score}";
            if (_timeLabel != null && TimerService != null) _timeLabel.text = $"{TimerService.Time:0.0}s";

            if (!_ending && _started && TimerService != null && !TimerService.IsRunning)
            {
                _ending = true;
                SceneService?.LoadAsync(GameOverScene);
            }
        }

        // -------------------- scene build --------------------

        void BuildCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            var t = cam.transform;
            t.position = new Vector3(0f, 16f, -12f);
            t.rotation = Quaternion.LookRotation((Vector3.zero - t.position).normalized, Vector3.up);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.1f);
        }

        void BuildFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.layer = PropLayer;
            floor.transform.localScale = new Vector3(ArenaRadius / 4f, 1f, ArenaRadius / 4f);
            Tint(floor, new Color(0.18f, 0.2f, 0.26f));
        }

        void BuildPlayer()
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Cube);
            player.name = "Player";
            player.layer = PlayerLayer;
            player.transform.position = new Vector3(0f, 0.5f, 0f);
            Tint(player, new Color(0.3f, 0.7f, 1f));

            var mover = player.AddComponent<Mover3D>();   // RequireComponent adds the Rigidbody
            mover.InputService = InputService;
            mover.MoveSpeed = 8f;
            mover.RotateToFaceMove = true;
        }

        GameObject BuildCollectiblePrefab()
        {
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            prefab.name = "Collectible";
            prefab.layer = PropLayer;
            prefab.transform.localScale = Vector3.one * 0.7f;
            Tint(prefab, new Color(1f, 0.85f, 0.25f));

            var col = prefab.GetComponent<SphereCollider>();
            col.isTrigger = true;

            var pickup = prefab.AddComponent<Pickup>();
            pickup.PoolService = PoolService;
            pickup.ScoreService = ScoreService;
            pickup.ScoreValue = 1;
            pickup.CollectorLayers = 1 << PlayerLayer;   // only the player collects

            var despawn = prefab.AddComponent<AutoDespawn>();
            despawn.PoolService = PoolService;
            despawn.Seconds = 8f;

            prefab.SetActive(false);
            return prefab;
        }

        void BuildSpawner(GameObject prefab)
        {
            var go = new GameObject("Spawner");
            go.transform.position = new Vector3(0f, 0.6f, 0f);
            var spawner = go.AddComponent<Spawner>();
            spawner.PoolService = PoolService;
            spawner.Prefab = prefab;
            spawner.Interval = SpawnInterval;
            spawner.MaxAlive = MaxAlive;
            spawner.Jitter = new Vector2(ArenaRadius, ArenaRadius);
            spawner.AutoStart = true;
        }

        void BuildHud()
        {
            var go = new GameObject("SurvivorHUD");
            var doc = go.AddComponent<UIDocument>();
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            doc.panelSettings = ps;

            var root = doc.rootVisualElement;
            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.justifyContent = Justify.SpaceBetween;
            bar.style.paddingLeft = bar.style.paddingRight = 24;
            bar.style.paddingTop = 16;
            root.Add(bar);

            _scoreLabel = HudLabel("Score: 0");
            _timeLabel = HudLabel("0.0s");
            bar.Add(_scoreLabel);
            bar.Add(_timeLabel);
        }

        static Label HudLabel(string text)
        {
            var l = new Label(text);
            l.style.fontSize = 36;
            l.style.color = Color.white;
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            return l;
        }

        static void Tint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = c;
        }
    }
}
