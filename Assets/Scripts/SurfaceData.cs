using UnityEngine;

[CreateAssetMenu(fileName = "SurfaceData", menuName = "VR Combat/Surface Data")]
public class SurfaceData : ScriptableObject
{
    public string surfaceTag;

    [Header("Visual Effects")]
    public GameObject impactEffectPrefab;
    public GameObject decalPrefab;
    public float decalLifetime = 10f;

    [Header("Audio Effects")]
    public AudioClip[] impactSounds;
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;
    public float volume = 0.8f;
}
