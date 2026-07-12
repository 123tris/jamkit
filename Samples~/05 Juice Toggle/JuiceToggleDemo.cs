using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

namespace Metz.JamKit.Samples
{
    /// <summary>
    /// Sample 05 — Juice Toggle. The before/after that sells Juice Lite: a turret pelts a target
    /// block twice a second; press J (or click the button) to flip every juice receiver on/off.
    /// With juice: camera kick, hit-stop, white flash, squash pop, damage numbers, debris burst,
    /// synthesized hit blips. Without: the same mechanics, dead on arrival.
    /// Drop on an empty GameObject and assign PoolService (+ AudioService for the blips).
    /// </summary>
    public sealed class JuiceToggleDemo : MonoBehaviour
    {
        [Header("Services (assign the wizard's SOs)")]
        public PoolServiceSO PoolService;
        [Tooltip("Optional — hit blips route through it (needs an AudioServiceRunner, e.g. JamKitCore).")]
        public AudioServiceSO AudioService;
        [Tooltip("Optional — hit-stop freeze frames (needs a TimeServiceRunner, e.g. JamKitCore).")]
        public TimeServiceSO TimeService;

        [Header("Tuning")]
        public float FireInterval = 0.5f;
        public float TargetHealth = 12f;

        bool _juiceOn = true;
        Button _toggleButton;
        GameObject _target;
        FloatingTextLayer _textLayer;

        void Awake()
        {
            BuildCamera();
            BuildFloor();
            _textLayer = BuildTextLayer();
            _target = BuildTarget();
            BuildTurret();
            BuildUI();
        }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.jKey.wasPressedThisFrame) SetJuice(!_juiceOn);
        }

        /// <summary>One switch, every receiver: JuiceBehaviour is the single seam to disable.</summary>
        public void SetJuice(bool on)
        {
            _juiceOn = on;
            foreach (var juice in FindObjectsByType<JuiceBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                juice.enabled = on;
            if (_toggleButton != null)
            {
                _toggleButton.text = on ? "JUICE: ON  (J)" : "JUICE: OFF  (J)";
                _toggleButton.style.backgroundColor = on ? new Color(0.95f, 0.6f, 0.1f) : new Color(0.25f, 0.27f, 0.32f);
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
            cam.transform.position = new Vector3(0f, 3.5f, -9f);
            cam.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
            // Standalone listener — works on a plain camera, no CinemachineCamera required.
            if (cam.GetComponent<CinemachineExternalImpulseListener>() == null)
                cam.gameObject.AddComponent<CinemachineExternalImpulseListener>();
        }

        void BuildFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(3f, 1f, 3f);
            Tint(floor, new Color(0.16f, 0.18f, 0.23f));
        }

        FloatingTextLayer BuildTextLayer()
        {
            var go = new GameObject("FloatingTextLayer");
            return go.AddComponent<FloatingTextLayer>();
        }

        GameObject BuildTarget()
        {
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "Target";
            target.transform.position = new Vector3(0f, 1.5f, 3f);
            target.transform.localScale = Vector3.one * 2f;
            Tint(target, new Color(0.85f, 0.3f, 0.35f));
            // Built inactive: JuiceBehaviour subscribes to Health in OnEnable, and the Reset()
            // sibling-trigger defaults are editor-only — for runtime AddComponent the flags must
            // be set by hand before OnEnable runs, so activation waits until the end.
            target.SetActive(false);

            var health = target.AddComponent<Health>();
            health.Max = health.Current = TargetHealth;

            // The full Juice Lite stack, all triggered by sibling damage/death:
            target.AddComponent<MaterialFlash>().OnSiblingDamage = true;
            var punch = target.AddComponent<PunchScale>();
            punch.OnSiblingDamage = true;
            punch.Punch = 0.85f; // squash, not pop — reads as "impact" on a big block

            var shake = target.AddComponent<CameraShake>();
            shake.OnSiblingDamage = true;
            shake.Force = 0.5f;

            var hitStop = target.AddComponent<HitStop>();
            hitStop.OnSiblingDamage = true;
            hitStop.TimeService = TimeService;

            var text = target.AddComponent<FloatingText>();
            text.OnSiblingDamage = true;
            text.Layer = _textLayer;
            text.ScaleByEventValue = true;
            text.WorldOffset = new Vector3(0f, 1.6f, 0f);

            var sfx = target.AddComponent<SfxOnEvent>();
            sfx.OnSiblingDamage = true;
            sfx.AudioService = AudioService;
            sfx.Clips = new[] { SynthBlip(220f), SynthBlip(196f), SynthBlip(247f) };

            var burst = target.AddComponent<SpawnBurst>();
            burst.OnSiblingDeath = true;
            burst.PoolService = PoolService;
            burst.Prefab = BuildDebrisPrefab();
            burst.Count = 10;
            burst.Scatter = 0.8f;
            burst.LaunchSpeed = 5f;

            // Death → debris burst, then refill so the show loops forever.
            health.Died += () => Invoke(nameof(RefillTarget), 0.5f);
            target.SetActive(true);
            return target;
        }

        void RefillTarget()
        {
            var h = _target != null ? _target.GetComponent<Health>() : null;
            if (h != null) h.ResetFull();
        }

        GameObject BuildDebrisPrefab()
        {
            var debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debris.name = "Debris";
            debris.transform.localScale = Vector3.one * 0.3f;
            Tint(debris, new Color(0.9f, 0.5f, 0.3f));
            debris.AddComponent<Rigidbody>();
            var despawn = debris.AddComponent<AutoDespawn>();
            despawn.PoolService = PoolService;
            despawn.Seconds = 1.5f;
            debris.SetActive(false);
            return debris;
        }

        void BuildTurret()
        {
            var turret = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            turret.name = "Turret";
            turret.transform.position = new Vector3(0f, 0.8f, -3.5f);
            turret.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
            Tint(turret, new Color(0.4f, 0.65f, 0.9f));

            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(turret.transform, false);
            muzzle.localPosition = new Vector3(0f, 0.8f, 0f);
            muzzle.rotation = Quaternion.LookRotation((new Vector3(0f, 1.5f, 3f) - muzzle.position).normalized);

            var shooter = turret.AddComponent<ProjectileShooter>();
            shooter.PoolService = PoolService;
            shooter.ProjectilePrefab = BuildProjectilePrefab();
            shooter.Muzzle = muzzle;
            shooter.UseAttackInput = false; // auto-fire
            shooter.FireInterval = FireInterval;
            shooter.Speed = 16f;
        }

        GameObject BuildProjectilePrefab()
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.name = "Bullet";
            p.transform.localScale = Vector3.one * 0.35f;
            Tint(p, new Color(1f, 0.9f, 0.4f));
            var rb = p.AddComponent<Rigidbody>();
            rb.useGravity = false;
            var dmg = p.AddComponent<Damager>();
            dmg.Damage = 1f;
            dmg.DestroyOnHit = true;
            dmg.PoolService = PoolService;
            var despawn = p.AddComponent<AutoDespawn>();
            despawn.PoolService = PoolService;
            despawn.Seconds = 3f;
            p.SetActive(false);
            return p;
        }

        void BuildUI()
        {
            var go = new GameObject("JuiceToggleUI");
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = JamKitUI.CreatePanelSettings(PanelScaleMode.ScaleWithScreenSize, sortingOrder: 10);

            _toggleButton = new Button(() => SetJuice(!_juiceOn));
            _toggleButton.style.position = Position.Absolute;
            _toggleButton.style.bottom = 32;
            _toggleButton.style.left = Length.Percent(50);
            _toggleButton.style.translate = new Translate(Length.Percent(-50), 0);
            _toggleButton.style.fontSize = 28;
            _toggleButton.style.paddingLeft = _toggleButton.style.paddingRight = 24;
            _toggleButton.style.paddingTop = _toggleButton.style.paddingBottom = 10;
            _toggleButton.style.color = Color.white;
            doc.rootVisualElement.Add(_toggleButton);
            SetJuice(true);
        }

        // -------------------- helpers --------------------

        /// <summary>Synthesized retro blip so the sample needs zero audio assets.</summary>
        static AudioClip SynthBlip(float frequency)
        {
            const int rate = 44100;
            int samples = rate / 12; // ~83 ms
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float envelope = 1f - (float)i / samples;
                data[i] = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t)) * 0.22f * envelope * envelope;
            }
            var clip = AudioClip.Create($"Blip{frequency:0}", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void Tint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = c;
        }
    }
}
