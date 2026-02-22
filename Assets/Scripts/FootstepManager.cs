using UnityEngine;
using System.Collections.Generic;

public class FootstepManager : MonoBehaviour
{
    [SerializeField] private List<SurfaceData> surfaces;
    [SerializeField] private SurfaceData defaultSurface;
    [SerializeField] private float stepThreshold = 0.5f;
    [SerializeField] private float stepInterval = 0.4f;

    private float nextStepTime;
    private AudioSource audioSource;
    private CharacterController controller;
    private Rigidbody rb;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        controller = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        float speed = GetVelocity();
        if (speed > stepThreshold && Time.time > nextStepTime)
        {
            PlayFootstep();
            nextStepTime = Time.time + (stepInterval / Mathf.Max(1f, speed / 3f));
        }
    }

    private float GetVelocity()
    {
        if (controller != null) return controller.velocity.magnitude;
        if (rb != null) return rb.linearVelocity.magnitude;
        return 0f;
    }

    private void PlayFootstep()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 0.5f))
        {
            SurfaceData data = surfaces.Find(s => s.surfaceTag == hit.collider.tag);
            if (data == null) data = defaultSurface;

            if (data != null && data.impactSounds.Length > 0)
            {
                AudioClip clip = data.impactSounds[Random.Range(0, data.impactSounds.Length)];
                audioSource.PlayOneShot(clip, data.volume * 0.5f);
            }
        }
    }
}
