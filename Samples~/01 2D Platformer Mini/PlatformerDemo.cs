using Ripple;
using Unity.Cinemachine;
using UnityEngine;

namespace Metz.JamKit.Samples
{
    /// <summary>
    /// Sample 01 — 2D Platformer Mini. Builds a self-contained 2D platformer scene at runtime.
    /// Demonstrates: Mover2D + InputServiceSO; Health + Ripple FloatEvent.
    ///
    /// For feel (sprite flash, screen shake, hit freeze) wire the Health's OnDamaged FloatEvent
    /// to an MMF_Player.PlayFeedbacks() call via UltEvents in the inspector. JamKit doesn't
    /// ship its own feel module any more — Feel does the job better.
    /// </summary>
    public sealed class PlatformerDemo : MonoBehaviour
    {
        [Header("Services")]
        public InputServiceSO InputService;

        [Header("Broadcast (Ripple, optional)")]
        public FloatEvent OnPlayerDamaged;
        public VoidEventSO OnPlayerDied;

        GameObject _player;

        void Awake()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
                camGo.AddComponent<CinemachineBrain>();
            }
            cam.orthographic = true;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);

            var ccGo = new GameObject("CMCam");
            var cmCam = ccGo.AddComponent<CinemachineCamera>();
            cmCam.Lens.OrthographicSize = 6f;

            BuildBlock(new Vector2(0, -3), new Vector2(20, 1), Color.gray, false);
            BuildBlock(new Vector2(-6, -1), new Vector2(4, 0.5f), Color.gray, false);
            BuildBlock(new Vector2(6, 0), new Vector2(4, 0.5f), Color.gray, false);

            _player = BuildBlock(new Vector2(0, 1), new Vector2(0.8f, 1.2f), new Color(0.6f, 0.9f, 1f), true);
            var rb = _player.GetComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            var mover = _player.AddComponent<Mover2D>();
            mover.InputService = InputService;

            var health = _player.AddComponent<Health>();
            health.Max = health.Current = 5;
            health.OnDamaged = OnPlayerDamaged;
            health.OnDied = OnPlayerDied;

            var enemy = BuildBlock(new Vector2(5, 1), new Vector2(0.8f, 0.8f), Color.red, true);
            enemy.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var dmg = enemy.AddComponent<Damager2D>();
            dmg.Damage = 1f;

            cmCam.Follow = _player.transform;
        }

        GameObject BuildBlock(Vector2 pos, Vector2 size, Color color, bool dynamic)
        {
            var go = new GameObject($"Block_{pos}");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhitePixelSprite();
            sr.color = color;
            sr.transform.localScale = new Vector3(size.x, size.y, 1f);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            if (dynamic)
            {
                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 3f;
            }
            return go;
        }

        static Sprite _whiteSprite;
        static Sprite WhitePixelSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _whiteSprite;
        }
    }
}
