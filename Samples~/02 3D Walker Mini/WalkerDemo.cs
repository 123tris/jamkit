using Ripple;
using Unity.Cinemachine;
using UnityEngine;

namespace Metz.JamKit.Samples
{
    /// <summary>
    /// Sample 02 — 3D Walker Mini. Builds a 3D walker scene at runtime.
    /// Demonstrates: Mover3D + InputServiceSO, AudioServiceSO.PlaySfx, AudioClipEvent firing.
    /// Wire the AudioClipEvent to a Feel MMF_Player with a floating-text feedback for polish.
    /// </summary>
    public sealed class WalkerDemo : MonoBehaviour
    {
        [Header("Services")]
        public InputServiceSO InputService;
        public AudioServiceSO AudioService;

        [Header("Sounds")]
        public AudioClip PickupClip;

        [Header("Broadcast (Ripple, optional)")]
        public AudioClipEvent OnPickup;

        GameObject _player;

        void Awake()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(4, 1, 4);
            ground.GetComponent<Renderer>().material.color = new Color(0.25f, 0.6f, 0.3f);

            _player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _player.transform.position = new Vector3(0, 1, 0);
            _player.GetComponent<Renderer>().material.color = new Color(0.6f, 0.8f, 1f);
            var rb = _player.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            var mover = _player.AddComponent<Mover3D>();
            mover.InputService = InputService;

            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
                camGo.AddComponent<CinemachineBrain>();
            }
            cam.transform.position = new Vector3(0, 6, -8);
            cam.transform.LookAt(_player.transform.position);

            var cmGo = new GameObject("CMCam");
            var cm = cmGo.AddComponent<CinemachineCamera>();
            cm.Follow = _player.transform;
            cm.LookAt = _player.transform;
            cmGo.transform.position = new Vector3(0, 6, -8);

            for (int i = 0; i < 8; i++)
            {
                var angle = i / 8f * Mathf.PI * 2f;
                var pos = new Vector3(Mathf.Cos(angle) * 6f, 0.5f, Mathf.Sin(angle) * 6f);
                BuildPickup(pos);
            }
        }

        void BuildPickup(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.6f;
            go.GetComponent<Renderer>().material.color = Color.yellow;
            var col = go.GetComponent<Collider>();
            col.isTrigger = true;
            var pickup = go.AddComponent<Pickup>();
            pickup.Owner = this;
        }

        public void OnPicked(Vector3 worldPos)
        {
            if (AudioService != null && PickupClip != null) AudioService.PlaySfx(PickupClip, 1f, 0.1f);
            if (OnPickup != null) OnPickup.Invoke(PickupClip);
        }

        class Pickup : MonoBehaviour
        {
            public WalkerDemo Owner;
            void OnTriggerEnter(Collider other)
            {
                if (Owner != null) Owner.OnPicked(transform.position);
                Destroy(gameObject);
            }
        }
    }
}
