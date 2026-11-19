using UnityEngine;

namespace Metz.JamKit
{
    [DisallowMultipleComponent]
    public sealed class Paddle : MonoBehaviour
    {
        [Tooltip("Scales the english this paddle imparts, on top of the ball's own English setting.")]
        [Range(0f, 2f)] public float EnglishMultiplier = 1f;
    }
}
