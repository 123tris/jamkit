namespace Metz.JamKit
{
    /// <summary>
    /// Implement on a MonoBehaviour to receive pool lifecycle callbacks.
    /// Called by <see cref="GameObjectPool"/> via GetComponents on spawn/despawn.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }
}
