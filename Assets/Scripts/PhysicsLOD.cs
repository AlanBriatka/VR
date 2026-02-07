using UnityEngine;
using System.Collections.Generic;

public class PhysicsLOD : MonoBehaviour
{
    [SerializeField] private float highFidelityDistance = 10f;
    [SerializeField] private float mediumFidelityDistance = 20f;
    [SerializeField] private float updateInterval = 0.5f;

    private Transform player;
    private List<Rigidbody> managedRigidbodies = new List<Rigidbody>();
    private float nextUpdateTime;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) player = playerObj.transform;
    }

    public void RegisterRigidbody(Rigidbody rb)
    {
        if (!managedRigidbodies.Contains(rb)) managedRigidbodies.Add(rb);
    }

    public void UnregisterRigidbody(Rigidbody rb)
    {
        managedRigidbodies.Remove(rb);
    }

    private void Update()
    {
        if (Time.time < nextUpdateTime || player == null) return;
        nextUpdateTime = Time.time + updateInterval;

        UpdateFidelity();
    }

    private void UpdateFidelity()
    {
        Vector3 playerPos = player.position;

        for (int i = managedRigidbodies.Count - 1; i >= 0; i--)
        {
            Rigidbody rb = managedRigidbodies[i];
            if (rb == null)
            {
                managedRigidbodies.RemoveAt(i);
                continue;
            }

            float distance = Vector3.Distance(rb.position, playerPos);

            if (distance > mediumFidelityDistance)
            {
                rb.interpolation = RigidbodyInterpolation.None;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                if (rb.linearVelocity.sqrMagnitude < 0.01f) rb.isKinematic = true;
            }
            else if (distance > highFidelityDistance)
            {
                rb.interpolation = RigidbodyInterpolation.None;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rb.isKinematic = false;
            }
            else
            {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.isKinematic = false;
            }
        }
    }
}
