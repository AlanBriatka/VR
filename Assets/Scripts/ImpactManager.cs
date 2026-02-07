using UnityEngine;
using System.Collections.Generic;

public class ImpactManager : MonoBehaviour
{
    [SerializeField] private List<SurfaceData> surfaces;
    [SerializeField] private SurfaceData defaultSurface;

    private static ImpactManager instance;
    public static ImpactManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ImpactManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("ImpactManager");
                    instance = obj.AddComponent<ImpactManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public void PlayImpact(Vector3 point, Vector3 normal, string tag, Transform parent = null)
    {
        SurfaceData data = surfaces.Find(s => s.surfaceTag == tag);
        if (data == null) data = defaultSurface;

        if (data == null) return;

        // Spawn particles
        if (data.impactEffectPrefab != null)
        {
            GameObject effect = Instantiate(data.impactEffectPrefab, point, Quaternion.LookRotation(normal));
            Destroy(effect, 2f);
        }

        // Spawn decal
        if (data.decalPrefab != null)
        {
            GameObject decal = Instantiate(data.decalPrefab, point + normal * 0.001f, Quaternion.LookRotation(-normal));
            if (parent != null) decal.transform.SetParent(parent);
            Destroy(decal, data.decalLifetime);
        }

        // Play sound
        if (data.impactSounds != null && data.impactSounds.Length > 0)
        {
            AudioClip clip = data.impactSounds[Random.Range(0, data.impactSounds.Length)];
            AudioSource.PlayClipAtPoint(clip, point, data.volume);
        }
    }
}
