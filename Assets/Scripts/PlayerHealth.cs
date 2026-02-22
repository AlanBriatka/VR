using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float healthRegenRate = 5f;
    [SerializeField] private float regenDelay = 5f;

    [Header("Visual Feedback")]
    [SerializeField] private Image damageVignette;
    [SerializeField] private AnimationCurve vignetteCurve;

    private float currentHealth;
    private float lastDamageTime;
    private static readonly WaitForFixedUpdate waitFixed = new WaitForFixedUpdate();

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        if (damageVignette != null)
        {
            Color c = damageVignette.color;
            c.a = 0;
            damageVignette.color = c;
        }
    }

    private void Update()
    {
        HandleRegen();
        UpdateVignette();
    }

    private void HandleRegen()
    {
        if (Time.time - lastDamageTime > regenDelay && currentHealth < maxHealth)
        {
            currentHealth = Mathf.MoveTowards(currentHealth, maxHealth, healthRegenRate * Time.deltaTime);
        }
    }

    private void UpdateVignette()
    {
        if (damageVignette == null) return;

        float healthRatio = 1f - (currentHealth / maxHealth);
        float targetAlpha = vignetteCurve.Evaluate(healthRatio);

        Color c = damageVignette.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * 2f);
        damageVignette.color = c;
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, DamageType damageType = DamageType.Bullet)
    {
        currentHealth -= damage;
        lastDamageTime = Time.time;

        // Trigger haptic warning on both controllers
        HapticsUtility.SendHapticImpulse(0.8f, 0.2f, HapticsUtility.Controller.Left);
        HapticsUtility.SendHapticImpulse(0.8f, 0.2f, HapticsUtility.Controller.Right);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player Died!");
        // Reload scene or show game over
    }
}
