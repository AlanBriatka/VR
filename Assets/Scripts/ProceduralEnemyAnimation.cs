using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ProceduralEnemyAnimation : MonoBehaviour
{
    [Header("Locomotion Settings")]
    [SerializeField] private float walkSpeedMultiplier = 2f;
    [SerializeField] private float strideLength = 0.3f;
    [SerializeField] private float strideHeight = 0.15f;
    [SerializeField] private float bodyBobHeight = 0.05f;

    [Header("IK Targets")]
    [SerializeField] private Transform leftFootTarget;
    [SerializeField] private Transform rightFootTarget;
    [SerializeField] private Transform bodyTarget;
    [SerializeField] private Transform headTarget;

    [Header("Idle Settings")]
    [SerializeField] private float breathingIntensity = 0.02f;
    [SerializeField] private float breathingSpeed = 1.5f;

    private Vector3 leftFootOriginalPos;
    private Vector3 rightFootOriginalPos;
    private Vector3 bodyOriginalPos;

    private float locomotionPhase;
    private float currentSpeed;
    private bool isAttacking;
    private float attackTime;

    private void Start()
    {
        if (leftFootTarget) leftFootOriginalPos = leftFootTarget.localPosition;
        if (rightFootTarget) rightFootOriginalPos = rightFootTarget.localPosition;
        if (bodyTarget) bodyOriginalPos = bodyTarget.localPosition;
    }

    public void UpdateAnimation(float speed)
    {
        currentSpeed = speed;

        if (speed > 0.1f)
        {
            UpdateLocomotion();
        }
        else
        {
            UpdateIdle();
        }

        if (isAttacking)
        {
            UpdateAttack();
        }
    }

    private void UpdateLocomotion()
    {
        locomotionPhase += Time.deltaTime * currentSpeed * walkSpeedMultiplier;

        float leftFootPhase = locomotionPhase;
        float rightFootPhase = locomotionPhase + Mathf.PI;

        if (leftFootTarget)
        {
            float x = 0;
            float y = Mathf.Max(0, Mathf.Sin(leftFootPhase)) * strideHeight;
            float z = Mathf.Cos(leftFootPhase) * strideLength;
            leftFootTarget.localPosition = leftFootOriginalPos + new Vector3(x, y, z);
        }

        if (rightFootTarget)
        {
            float x = 0;
            float y = Mathf.Max(0, Mathf.Sin(rightFootPhase)) * strideHeight;
            float z = Mathf.Cos(rightFootPhase) * strideLength;
            rightFootTarget.localPosition = rightFootOriginalPos + new Vector3(x, y, z);
        }

        if (bodyTarget)
        {
            float bob = Mathf.Abs(Mathf.Sin(locomotionPhase * 2f)) * bodyBobHeight;
            bodyTarget.localPosition = bodyOriginalPos + new Vector3(0, bob, 0);
        }
    }

    private void UpdateIdle()
    {
        float breath = Mathf.Sin(Time.time * breathingSpeed) * breathingIntensity;
        if (bodyTarget)
        {
            bodyTarget.localPosition = Vector3.Lerp(bodyTarget.localPosition, bodyOriginalPos + new Vector3(0, breath, 0), Time.deltaTime * 5f);
        }

        // Reset feet
        if (leftFootTarget) leftFootTarget.localPosition = Vector3.Lerp(leftFootTarget.localPosition, leftFootOriginalPos, Time.deltaTime * 5f);
        if (rightFootTarget) rightFootTarget.localPosition = Vector3.Lerp(rightFootTarget.localPosition, rightFootOriginalPos, Time.deltaTime * 5f);
    }

    public void PlayAttack()
    {
        if (!isAttacking)
        {
            isAttacking = true;
            attackTime = 0f;
        }
    }

    private void UpdateAttack()
    {
        attackTime += Time.deltaTime * 5f;
        if (attackTime > Mathf.PI)
        {
            isAttacking = false;
            attackTime = 0f;
            return;
        }

        // Simple lunge forward
        float lunge = Mathf.Sin(attackTime) * 0.4f;
        if (bodyTarget)
        {
            bodyTarget.localPosition += Vector3.forward * lunge;
        }
    }
}
