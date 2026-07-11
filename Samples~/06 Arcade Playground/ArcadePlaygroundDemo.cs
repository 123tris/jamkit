using UnityEngine;

namespace Metz.JamKit.Samples
{
    /// <summary>
    /// Sample 06 — Arcade Playground. One 2D scene exercising the whole any-genre kit together:
    ///   • Frogger crossing: <see cref="GridMover"/> player, <see cref="PatrolMover"/> cars on a
    ///     conveyor (TeleportToStart), a river of <see cref="TriggerZone"/> kill water with a
    ///     bridge, a one-shot goal zone, and a <see cref="Respawner"/>.
    ///   • An <see cref="Interactable"/> lever (press E near it) that toggles traffic speed.
    ///   • A self-playing breakout pit: <see cref="Bouncer2D"/> ball, an angled <see cref="Paddle"/>
    ///     wall, and brick rows (Health 1 + SpriteFlash + SpawnBurst debris) that rebuild when cleared.
    /// Everything is tinted white-square sprites — no art, no extra assets.
    /// Drop on an empty GameObject and assign InputService (+ optional Score/Pool services).
    /// </summary>
    public sealed class ArcadePlaygroundDemo : MonoBehaviour
    {
        [Header("Services (assign the wizard's SOs)")]
        public InputServiceSO InputService;
        [Tooltip("Optional — goal and bricks award points when set.")]
        public ScoreServiceSO ScoreService;
        [Tooltip("Optional — debris and cars pool when set.")]
        public PoolServiceSO PoolService;

        const int BlockLayer = 4; // built-in Water layer, reused as "solid for GridMover"

        Sprite _square;
        FloatingTextLayer _textLayer;
        Toast _toast;
        GameObject _player;
        readonly System.Collections.Generic.List<PatrolMover> _cars = new();
        readonly System.Collections.Generic.List<Health> _bricks = new();
        bool _fastTraffic = true;
        float _rebuildAt = -1f;

        void Awake()
        {
            _square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);

            BuildCamera();
            _textLayer = new GameObject("FloatingTextLayer").AddComponent<FloatingTextLayer>();
            _toast = new GameObject("Toast").AddComponent<Toast>();

            BuildFroggerLanes();
            BuildLever();
            BuildPlayer();
            BuildBreakoutPit();
        }

        void Update()
        {
            if (_rebuildAt > 0f && Time.time >= _rebuildAt)
            {
                _rebuildAt = -1f;
                BuildBrickRows();
                _toast.Show("Bricks reset!");
            }
            else if (_rebuildAt < 0f && _bricks.Count > 0 && AllBricksDead())
            {
                _rebuildAt = Time.time + 1.5f;
            }
        }

        bool AllBricksDead()
        {
            foreach (var b in _bricks)
                if (b != null && !b.IsDead) return false;
            return true;
        }

        // -------------------- camera --------------------

        void BuildCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.transform.position = new Vector3(2.5f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        }

        // -------------------- frogger side (x -6..3) --------------------

        void BuildFroggerLanes()
        {
            // Ground stripes for readability.
            Stripe(-5f, new Color(0.15f, 0.2f, 0.15f));   // start grass
            Stripe(-3f, new Color(0.2f, 0.2f, 0.22f));    // road 1
            Stripe(-2f, new Color(0.2f, 0.2f, 0.22f));    // road 2
            Stripe(0f, new Color(0.15f, 0.2f, 0.15f));    // median
            Stripe(2f, new Color(0.12f, 0.2f, 0.3f));     // river
            Stripe(4f, new Color(0.25f, 0.22f, 0.12f));   // goal bank

            // Cars: conveyor patrols crossing the lanes in opposite directions.
            for (int i = 0; i < 3; i++)
            {
                _cars.Add(BuildCar(new Vector3(-6f + i * 3.5f, -3f, 0f), +1));
                _cars.Add(BuildCar(new Vector3(3f - i * 3.5f, -2f, 0f), -1));
            }

            // River: two kill zones with a one-cell bridge at x = -1.
            BuildKillWater(new Vector3(-4f, 2f, 0f), new Vector2(5f, 2f));
            BuildKillWater(new Vector3(1.5f, 2f, 0f), new Vector2(4f, 2f));
            // Bridge rails keep the GridMover honest: only the gap column crosses.
            BuildBlocked(new Vector3(-2f, 2f, 0f), new Vector2(0.9f, 2f));
            BuildBlocked(new Vector3(0f, 2f, 0f), new Vector2(0.9f, 2f));

            // Goal: one-shot zone on the far bank.
            var goal = MakeSprite("Goal", new Vector3(-1f, 4f, 0f), new Vector2(2f, 1f), new Color(1f, 0.85f, 0.3f, 0.65f));
            var goalCol = goal.AddComponent<BoxCollider2D>();
            goalCol.isTrigger = true;
            var zone = goal.AddComponent<TriggerZone>();
            zone.RequiredTag = "Player";
            zone.OneShot = false;
            zone.ScoreService = ScoreService;
            zone.ScoreValue = 100;
            zone.Entered += _ =>
            {
                _toast.Show("GOAL!  +100");
                _player.GetComponent<Respawner>().Respawn();
            };
        }

        void Stripe(float y, Color c) => MakeSprite($"Stripe y={y}", new Vector3(-1.5f, y, 1f), new Vector2(9f, y is 2f ? 2f : 1f), c);

        PatrolMover BuildCar(Vector3 pos, int dir)
        {
            var car = MakeSprite("Car", pos, new Vector2(1.4f, 0.8f), new Color(1f, 0.55f, 0.2f));
            var rb = car.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var col = car.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            var patrol = car.AddComponent<PatrolMover>();
            patrol.PathOffsets = new[] { new Vector3(dir * 9f, 0f, 0f) };
            patrol.Mode = PatrolMover.EndMode.TeleportToStart;
            patrol.Speed = 3.5f;

            var damage = car.AddComponent<Damager2D>();
            damage.Damage = 999f;
            return patrol;
        }

        void BuildKillWater(Vector3 pos, Vector2 size)
        {
            var water = new GameObject("KillWater");
            water.transform.position = pos;
            var col = water.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            var zone = water.AddComponent<TriggerZone>();
            zone.RequiredTag = "Player";
            zone.Kill = true;
        }

        void BuildBlocked(Vector3 pos, Vector2 size)
        {
            var block = new GameObject("Blocked") { layer = BlockLayer };
            block.transform.position = pos;
            var col = block.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        void BuildLever()
        {
            var lever = MakeSprite("Lever", new Vector3(1f, 0f, 0f), new Vector2(0.6f, 0.9f), new Color(0.7f, 0.5f, 1f));
            var col = lever.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            // Prompt: a small floating marker the Interactor toggles — zero UI plumbing.
            var prompt = MakeSprite("Prompt", lever.transform.position + new Vector3(0f, 0.9f, 0f), new Vector2(0.35f, 0.35f), Color.white);
            prompt.transform.SetParent(lever.transform, true);
            prompt.SetActive(false);

            var interactable = lever.AddComponent<Interactable>();
            interactable.PromptVisual = prompt;
            interactable.Interacted += _ =>
            {
                _fastTraffic = !_fastTraffic;
                foreach (var car in _cars)
                    if (car != null) car.Speed = _fastTraffic ? 3.5f : 1.2f;
                _toast.Show(_fastTraffic ? "Traffic: FAST" : "Traffic: slow");
            };
        }

        void BuildPlayer()
        {
            _player = MakeSprite("Player", new Vector3(-1f, -5f, 0f), new Vector2(0.8f, 0.8f), new Color(0.4f, 0.95f, 0.5f));
            _player.tag = "Player";
            var col = _player.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            // Static trigger zones (water, goal) only fire when one side has a Rigidbody2D.
            var rb = _player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var mover = _player.AddComponent<GridMover>();
            mover.InputService = InputService;
            mover.BlockedBy = 1 << BlockLayer;

            var health = _player.AddComponent<Health>();
            health.Max = health.Current = 1f;

            var respawner = _player.AddComponent<Respawner>();
            respawner.Delay = 0.5f;
            health.Died += () => _toast.Show("Splat.");

            _player.AddComponent<SpriteFlash>();
            _player.AddComponent<PunchScale>();

            var interactor = _player.AddComponent<Interactor>();
            interactor.InputService = InputService;
            interactor.Radius = 1.4f;
        }

        // -------------------- breakout side (x 5..10) --------------------

        void BuildBreakoutPit()
        {
            var wallColor = new Color(0.45f, 0.48f, 0.55f);
            BuildWall(new Vector3(7.5f, 5.5f, 0f), new Vector2(6f, 0.5f), wallColor);          // top
            BuildWall(new Vector3(7.5f, -5.5f, 0f), new Vector2(6f, 0.5f), wallColor);         // bottom
            BuildWall(new Vector3(4.75f, 0f, 0f), new Vector2(0.5f, 11.5f), wallColor);        // left
            BuildWall(new Vector3(10.25f, 0f, 0f), new Vector2(0.5f, 11.5f), wallColor);       // right

            // One angled paddle wall — english bends every bounce differently, keeping it lively.
            var paddle = BuildWall(new Vector3(7.5f, -4.4f, 0f), new Vector2(2.6f, 0.4f), new Color(0.95f, 0.95f, 1f));
            paddle.transform.rotation = Quaternion.Euler(0f, 0f, 8f);
            paddle.AddComponent<Paddle>();

            var ball = MakeSprite("Ball", new Vector3(7.5f, -2f, 0f), new Vector2(0.5f, 0.5f), Color.white);
            ball.AddComponent<CircleCollider2D>();
            ball.AddComponent<Rigidbody2D>();
            var bouncer = ball.AddComponent<Bouncer2D>();
            bouncer.Speed = 7f;
            bouncer.LaunchDirection = new Vector2(0.7f, 1f);

            var damager = ball.AddComponent<Damager2D>();
            damager.Damage = 1f;

            BuildBrickRows();
        }

        GameObject BuildWall(Vector3 pos, Vector2 size, Color c)
        {
            var wall = MakeSprite("Wall", pos, size, c);
            wall.AddComponent<BoxCollider2D>();
            return wall;
        }

        void BuildBrickRows()
        {
            _bricks.Clear();
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 5; i++)
                {
                    var pos = new Vector3(5.6f + i * 0.95f, 4.6f - row * 0.6f, 0f);
                    var brick = MakeSprite("Brick", pos, new Vector2(0.85f, 0.5f),
                        Color.HSVToRGB(0.55f + row * 0.09f, 0.6f, 0.95f));
                    brick.AddComponent<BoxCollider2D>();

                    var health = brick.AddComponent<Health>();
                    health.Max = health.Current = 1f;
                    health.DestroyOnDeath = true;

                    brick.AddComponent<SpriteFlash>();

                    var text = brick.AddComponent<FloatingText>();
                    text.Layer = _textLayer;
                    text.Format = "+10";
                    text.OnSiblingDamage = false;
                    text.OnSiblingDeath = true;

                    var burst = brick.AddComponent<SpawnBurst>();
                    burst.PoolService = PoolService;
                    burst.Prefab = GetDebrisPrefab();
                    burst.Count = 4;
                    burst.Scatter = 0.3f;
                    burst.Is2D = true;
                    burst.LaunchSpeed = 3f;

                    if (ScoreService != null)
                        health.Died += () => ScoreService.Add(10);

                    _bricks.Add(health);
                }
        }

        GameObject _debrisPrefab;

        GameObject GetDebrisPrefab()
        {
            if (_debrisPrefab != null) return _debrisPrefab;
            _debrisPrefab = MakeSprite("Debris", Vector3.zero, new Vector2(0.2f, 0.2f), new Color(0.9f, 0.8f, 0.5f));
            var rb = _debrisPrefab.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1.5f;
            var despawn = _debrisPrefab.AddComponent<AutoDespawn>();
            despawn.PoolService = PoolService;
            despawn.Seconds = 1.2f;
            _debrisPrefab.SetActive(false);
            return _debrisPrefab;
        }

        // -------------------- helpers --------------------

        GameObject MakeSprite(string name, Vector3 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _square;
            sr.color = color;
            return go;
        }
    }
}
