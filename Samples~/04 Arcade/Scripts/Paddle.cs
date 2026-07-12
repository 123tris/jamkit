using UnityEngine;

namespace Metz.JamKit
{
    /// <summary>
    /// Marker for <see cref="Bouncer2D"/> english: balls bouncing off an object carrying this
    /// component get their outgoing angle bent by where along the paddle they hit (edge hits cut
    /// sharper — the pong/breakout aiming mechanic). A component beats a layer mask here: layers
    /// are project-global state the kit shouldn't own, and a marker auto-wires with zero setup.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Paddle : MonoBehaviour
    {
        [Tooltip("Scales the english this paddle imparts, on top of the ball's own English setting.")]
        [Range(0f, 2f)] public float EnglishMultiplier = 1f;
    }
}
