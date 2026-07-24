using System;
using Ripple;
using UltEvents;
using UnityEngine;

public class HitHandler : MonoBehaviour
{
    public Action<GameObject> OnHit;

    [SerializeField] private UltEvent<GameObject> onHitEvent;

    [SerializeField] private GameObjectEvent broadCastEvent;

    private void Awake()
    {
        if (onHitEvent != null)
            OnHit += onHitEvent.Invoke;
        if (broadCastEvent)
            OnHit += g => broadCastEvent.Invoke(g, this);
    }

    private void OnDestroy()
    {
        OnHit -= onHitEvent.Invoke;
    }

    void OnTriggerEnter(Collider other) => OnHit?.Invoke(other.gameObject);
    void OnCollisionEnter(Collision other) => OnHit?.Invoke(other.gameObject);
}