using Ripple;
using UnityEngine;

namespace Metz.JamKit.Samples
{
    /// <summary>
    /// Sample 00 — Bootstrap. Demonstrates: AudioServiceSO injection, PoolServiceSO spawning,
    /// Ripple VoidEventSO firing. Wire the VoidEventSO to a Feel MMF_Player in the inspector
    /// to add screen shake / hit-freeze / sound on click — JamKit no longer ships those.
    /// </summary>
    public sealed class BootstrapDemo : MonoBehaviour
    {
        [Header("Services")]
        public InputServiceSO InputService;
        public PoolServiceSO PoolService;
        public AudioServiceSO AudioService;

        [Header("Spawning")]
        public GameObject CubePrefab;
        public AudioClip ClickClip;

        [Header("Events (Ripple)")]
        [Tooltip("Raised whenever the user clicks. Wire to MMF_Player.PlayFeedbacks() for feel.")]
        public VoidEventSO OnClicked;

        Camera _cam;

        void Awake()
        {
            _cam = Camera.main;
            if (CubePrefab == null)
            {
                CubePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                CubePrefab.SetActive(false);
                CubePrefab.AddComponent<Rigidbody>();
            }
        }

        void Update()
        {
            if (InputService == null) return;
            var attack = InputService.Attack;
            if (attack == null || !attack.WasPressedThisFrame()) return;

            Vector3 pos = _cam != null ? _cam.transform.position + _cam.transform.forward * 6f : transform.position;
            if (PoolService != null) PoolService.Spawn(CubePrefab, pos, Random.rotation);
            else Instantiate(CubePrefab, pos, Random.rotation).SetActive(true);

            if (AudioService != null && ClickClip != null) AudioService.PlaySfx(ClickClip);
            if (OnClicked != null) OnClicked.Invoke();
        }
    }
}
