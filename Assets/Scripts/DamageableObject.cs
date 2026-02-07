using UnityEngine;

public class DamageableObject : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    
    [Header("Physics Response")]
    public bool applyForceOnHit = true;
    public float forceMultiplier = 10f;
    
    [Header("Visual Feedback")]
    public GameObject bloodEffectPrefab;
    public Color damageColor = Color.red;
    
    private Rigidbody rb;
    private Renderer objectRenderer;
    private Color originalColor;
    
    private void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        objectRenderer = GetComponent<Renderer>();
        
        if (objectRenderer != null)
        {
            originalColor = objectRenderer.material.color;
        }
    }
    
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, DamageType damageType = DamageType.Slash)
    {
        currentHealth -= damage;
        
        string damageTypeStr = damageType.ToString().ToUpper();
        Debug.Log($"[{damageTypeStr}] {gameObject.name} took {damage:F1} damage. Health: {currentHealth:F0}/{maxHealth}");
        
        if (applyForceOnHit && rb != null)
        {
            float forceMult = damageType == DamageType.Bullet ? forceMultiplier * 0.5f : forceMultiplier;
            rb.AddForceAtPosition(hitDirection * damage * forceMult, hitPoint, ForceMode.Impulse);
        }
        
        ShowDamageEffect(hitPoint, damageType);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    private void ShowDamageEffect(Vector3 hitPoint, DamageType damageType)
    {
        if (objectRenderer != null)
        {
            StartCoroutine(FlashDamageColor());
        }
        
        if (bloodEffectPrefab != null)
        {
            GameObject effect = Instantiate(bloodEffectPrefab, hitPoint, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    private System.Collections.IEnumerator FlashDamageColor()
    {
        if (objectRenderer != null)
        {
            objectRenderer.material.color = damageColor;
            yield return new WaitForSeconds(0.1f);
            objectRenderer.material.color = originalColor;
        }
    }
    
    private void Die()
    {
        Debug.Log($"{gameObject.name} has been destroyed!");
        Destroy(gameObject, 2f);
    }
}
