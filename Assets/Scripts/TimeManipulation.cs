using UnityEngine;
using UnityEngine.InputSystem;

public class TimeManipulation : MonoBehaviour
{
    [Header("Time Control")]
    [SerializeField] private float slowMotionScale = 0.2f;
    [SerializeField] private float transitionSpeed = 5f;
    
    [Header("Input")]
    [SerializeField] private InputActionReference slowMotionAction;
    
    [Header("Audio")]
    [SerializeField] private AudioClip slowMotionEnterSound;
    [SerializeField] private AudioClip slowMotionExitSound;
    
    private float targetTimeScale = 1f;
    private float currentTimeScale = 1f;
    private bool isSlowMotion;
    private AudioSource audioSource;
    
    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
    }
    
    private void OnEnable()
    {
        if (slowMotionAction != null)
        {
            slowMotionAction.action.performed += OnSlowMotionToggle;
            slowMotionAction.action.Enable();
        }
    }
    
    private void OnDisable()
    {
        if (slowMotionAction != null)
        {
            slowMotionAction.action.performed -= OnSlowMotionToggle;
            slowMotionAction.action.Disable();
        }
    }
    
    private void Update()
    {
        UpdateTimeScale();
    }
    
    private void UpdateTimeScale()
    {
        currentTimeScale = Mathf.Lerp(currentTimeScale, targetTimeScale, Time.unscaledDeltaTime * transitionSpeed);
        Time.timeScale = currentTimeScale;
        Time.fixedDeltaTime = 0.02f * currentTimeScale;
    }
    
    private void OnSlowMotionToggle(InputAction.CallbackContext context)
    {
        ToggleSlowMotion();

        if (context.control != null && context.control.device != null)
        {
            bool isLeft = context.control.device.name.ToLower().Contains("left");
            HapticsUtility.SendHapticImpulse(0.3f, 0.15f, isLeft ? HapticsUtility.Controller.Left : HapticsUtility.Controller.Right);
        }
    }
    
    public void ToggleSlowMotion()
    {
        isSlowMotion = !isSlowMotion;
        targetTimeScale = isSlowMotion ? slowMotionScale : 1f;
        
        PlaySound(isSlowMotion ? slowMotionEnterSound : slowMotionExitSound);
    }
    
    public void EnableSlowMotion()
    {
        if (!isSlowMotion)
        {
            isSlowMotion = true;
            targetTimeScale = slowMotionScale;
            PlaySound(slowMotionEnterSound);
        }
    }
    
    public void DisableSlowMotion()
    {
        if (isSlowMotion)
        {
            isSlowMotion = false;
            targetTimeScale = 1f;
            PlaySound(slowMotionExitSound);
        }
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    private void OnApplicationQuit()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
