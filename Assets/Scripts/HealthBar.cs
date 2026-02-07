using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    public IDamageable target;
    public Image healthBarFill;
    
    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 2f, 0);
    public bool alwaysFaceCamera = true;
    
    private Camera mainCamera;
    private Transform targetTransform;
    
    private void Start()
    {
        mainCamera = Camera.main;
        
        if (target == null)
        {
            target = GetComponentInParent<IDamageable>();
        }
        
        if (target != null && target is MonoBehaviour)
        {
            targetTransform = ((MonoBehaviour)target).transform;
        }
    }
    
    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }
        
        if (targetTransform != null)
        {
            transform.position = targetTransform.position + offset;
        }
        
        if (alwaysFaceCamera && mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                            mainCamera.transform.rotation * Vector3.up);
        }
        
        UpdateHealthBar();
    }
    
    private void UpdateHealthBar()
    {
        if (healthBarFill != null && target != null)
        {
            float healthPercent = target.GetCurrentHealth() / target.GetMaxHealth();
            healthBarFill.fillAmount = healthPercent;
            
            healthBarFill.color = Color.Lerp(Color.red, Color.green, healthPercent);
        }
    }
}
