using UnityEngine;
using System.Collections;

public class ComboManager : MonoBehaviour
{
    [SerializeField] private float comboExpiryTime = 2f;
    [SerializeField] private AudioClip comboSound;
    [SerializeField] private float pitchIncreasePerKill = 0.1f;

    private int currentCombo = 0;
    private float lastKillTime;
    private AudioSource audioSource;

    private static ComboManager instance;
    public static ComboManager Instance => instance;

    private void Awake()
    {
        instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void RegisterKill()
    {
        if (Time.time - lastKillTime < comboExpiryTime)
        {
            currentCombo++;
        }
        else
        {
            currentCombo = 1;
        }

        lastKillTime = Time.time;
        PlayComboJuice();
    }

    private void PlayComboJuice()
    {
        // Haptic burst on both hands
        HapticsUtility.SendHapticImpulse(0.4f + (currentCombo * 0.05f), 0.1f, HapticsUtility.Controller.Left);
        HapticsUtility.SendHapticImpulse(0.4f + (currentCombo * 0.05f), 0.1f, HapticsUtility.Controller.Right);

        if (comboSound != null)
        {
            audioSource.pitch = 1f + (currentCombo * pitchIncreasePerKill);
            audioSource.PlayOneShot(comboSound, 0.7f);
        }

        Debug.Log($"COMBO X{currentCombo}!");
    }
}
