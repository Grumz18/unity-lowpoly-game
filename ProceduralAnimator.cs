using UnityEngine;

[DisallowMultipleComponent]
public class ProceduralAnimator : MonoBehaviour
{
    [Header("Body Parts")]
    public Transform armLeft;
    public Transform armRight;
    public Transform legLeft;
    public Transform legRight;
    public Transform head;
    public Transform torso;

    [Header("Rest Pose Offsets (Euler)")]
    public Vector3 armLeftRestOffset = Vector3.zero;
    public Vector3 armRightRestOffset = Vector3.zero;
    public Vector3 legLeftRestOffset = Vector3.zero;
    public Vector3 legRightRestOffset = Vector3.zero;

    [Header("Walk")]
    public Vector3 armSwingAxis = Vector3.right;
    public Vector3 legSwingAxis = Vector3.right;
    public float armSwingAngle = 22f;
    public float legSwingAngle = 28f;
    public float walkCycleSpeed = 8f;
    public float runCycleMultiplier = 1.35f;
    public float movementThreshold = 0.05f;

    [Header("Idle")]
    public float idleArmSway = 3f;
    public float idleBobAmount = 0.02f;
    public float idleBobSpeed = 2f;

    [Header("Jump")]
    public float jumpLegBend = 16f;
    public float jumpArmBack = 10f;

    [Header("Smoothing")]
    public float limbLerpSpeed = 14f;

    private PlayerController player;
    private float cycle;

    private Quaternion armLeftBaseRot;
    private Quaternion armRightBaseRot;
    private Quaternion legLeftBaseRot;
    private Quaternion legRightBaseRot;
    private Quaternion headBaseRot;
    private Quaternion torsoBaseRot;
    private Vector3 torsoBasePos;

    private Quaternion armLeftRestRot;
    private Quaternion armRightRestRot;
    private Quaternion legLeftRestRot;
    private Quaternion legRightRestRot;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        CacheBasePose();
        RebuildRestPose();
        ApplyRestPoseImmediate();
    }

    void OnValidate()
    {
        NormalizeAxis(ref armSwingAxis, Vector3.right);
        NormalizeAxis(ref legSwingAxis, Vector3.right);

        if (!Application.isPlaying)
        {
            if (armLeft != null && armLeftBaseRot == Quaternion.identity) armLeftBaseRot = armLeft.localRotation;
            if (armRight != null && armRightBaseRot == Quaternion.identity) armRightBaseRot = armRight.localRotation;
            if (legLeft != null && legLeftBaseRot == Quaternion.identity) legLeftBaseRot = legLeft.localRotation;
            if (legRight != null && legRightBaseRot == Quaternion.identity) legRightBaseRot = legRight.localRotation;
            if (head != null && headBaseRot == Quaternion.identity) headBaseRot = head.localRotation;
            if (torso != null && torsoBaseRot == Quaternion.identity)
            {
                torsoBaseRot = torso.localRotation;
                torsoBasePos = torso.localPosition;
            }

            RebuildRestPose();
            ApplyRestPoseImmediate();
        }
    }

    [ContextMenu("Use Current Pose As Base")]
    void UseCurrentPoseAsBase()
    {
        CacheBasePose();
        RebuildRestPose();
        ApplyRestPoseImmediate();
    }

    [ContextMenu("Rebuild Rest Pose")]
    void RebuildRestPose()
    {
        armLeftRestRot = armLeftBaseRot * Quaternion.Euler(armLeftRestOffset);
        armRightRestRot = armRightBaseRot * Quaternion.Euler(armRightRestOffset);
        legLeftRestRot = legLeftBaseRot * Quaternion.Euler(legLeftRestOffset);
        legRightRestRot = legRightBaseRot * Quaternion.Euler(legRightRestOffset);
    }

    void LateUpdate()
    {
        if (player == null)
        {
            player = GetComponent<PlayerController>();
        }

        float speed = player != null ? Mathf.Clamp01(player.currentSpeed) : 0f;
        bool grounded = player == null || player.isGrounded;
        bool jumping = player != null && player.isJumping && !grounded;
        bool running = player != null && player.isRunning;

        if (jumping)
        {
            AnimateJump();
            return;
        }

        if (grounded && speed > movementThreshold)
        {
            AnimateWalk(speed, running);
        }
        else
        {
            AnimateIdle();
        }
    }

    void AnimateWalk(float speed, bool running)
    {
        float cycleMultiplier = running ? runCycleMultiplier : 1f;
        float speedFactor = Mathf.Lerp(0.45f, 1f, speed);

        cycle += Time.deltaTime * walkCycleSpeed * cycleMultiplier;

        float s = Mathf.Sin(cycle);
        float c = Mathf.Cos(cycle);

        SetLimbRotation(legLeft, legLeftRestRot, legSwingAxis, s * legSwingAngle * speedFactor);
        SetLimbRotation(legRight, legRightRestRot, legSwingAxis, -s * legSwingAngle * speedFactor);

        SetLimbRotation(armLeft, armLeftRestRot, armSwingAxis, -s * armSwingAngle * speedFactor);
        SetLimbRotation(armRight, armRightRestRot, armSwingAxis, s * armSwingAngle * speedFactor);

        if (torso != null)
        {
            float bob = Mathf.Abs(c) * idleBobAmount * 1.5f * speedFactor;
            torso.localPosition = torsoBasePos + Vector3.up * bob;
            torso.localRotation = Quaternion.Slerp(
                torso.localRotation,
                torsoBaseRot * Quaternion.Euler(running ? 4f : 1.5f, 0f, s * 1.5f * speedFactor),
                Time.deltaTime * limbLerpSpeed);
        }

        if (head != null)
        {
            head.localRotation = Quaternion.Slerp(head.localRotation, headBaseRot, Time.deltaTime * limbLerpSpeed);
        }
    }

    void AnimateIdle()
    {
        cycle += Time.deltaTime * idleBobSpeed;
        float s = Mathf.Sin(cycle);

        SetLimbRotation(armLeft, armLeftRestRot, armSwingAxis, s * idleArmSway);
        SetLimbRotation(armRight, armRightRestRot, armSwingAxis, -s * idleArmSway);

        SetLimbRotation(legLeft, legLeftRestRot, legSwingAxis, 0f);
        SetLimbRotation(legRight, legRightRestRot, legSwingAxis, 0f);

        if (torso != null)
        {
            float bob = s * idleBobAmount;
            torso.localPosition = torsoBasePos + Vector3.up * bob;
            torso.localRotation = Quaternion.Slerp(torso.localRotation, torsoBaseRot, Time.deltaTime * limbLerpSpeed);
        }

        if (head != null)
        {
            head.localRotation = Quaternion.Slerp(head.localRotation, headBaseRot, Time.deltaTime * limbLerpSpeed);
        }
    }

    void AnimateJump()
    {
        SetLimbRotation(legLeft, legLeftRestRot, legSwingAxis, jumpLegBend);
        SetLimbRotation(legRight, legRightRestRot, legSwingAxis, jumpLegBend);

        SetLimbRotation(armLeft, armLeftRestRot, armSwingAxis, -jumpArmBack);
        SetLimbRotation(armRight, armRightRestRot, armSwingAxis, -jumpArmBack);

        if (torso != null)
        {
            torso.localPosition = Vector3.Lerp(torso.localPosition, torsoBasePos, Time.deltaTime * limbLerpSpeed);
            torso.localRotation = Quaternion.Slerp(torso.localRotation, torsoBaseRot, Time.deltaTime * limbLerpSpeed);
        }

        if (head != null)
        {
            head.localRotation = Quaternion.Slerp(head.localRotation, headBaseRot, Time.deltaTime * limbLerpSpeed);
        }
    }

    void SetLimbRotation(Transform limb, Quaternion restRot, Vector3 axis, float angle)
    {
        if (limb == null)
        {
            return;
        }

        Quaternion target = restRot * Quaternion.AngleAxis(angle, SafeAxis(axis));
        limb.localRotation = Quaternion.Slerp(limb.localRotation, target, Time.deltaTime * limbLerpSpeed);
    }

    void CacheBasePose()
    {
        if (armLeft != null) armLeftBaseRot = armLeft.localRotation;
        if (armRight != null) armRightBaseRot = armRight.localRotation;
        if (legLeft != null) legLeftBaseRot = legLeft.localRotation;
        if (legRight != null) legRightBaseRot = legRight.localRotation;
        if (head != null) headBaseRot = head.localRotation;
        if (torso != null)
        {
            torsoBaseRot = torso.localRotation;
            torsoBasePos = torso.localPosition;
        }
    }

    void ApplyRestPoseImmediate()
    {
        if (armLeft != null) armLeft.localRotation = armLeftRestRot;
        if (armRight != null) armRight.localRotation = armRightRestRot;
        if (legLeft != null) legLeft.localRotation = legLeftRestRot;
        if (legRight != null) legRight.localRotation = legRightRestRot;
        if (head != null) head.localRotation = headBaseRot;
        if (torso != null)
        {
            torso.localRotation = torsoBaseRot;
            torso.localPosition = torsoBasePos;
        }
    }

    Vector3 SafeAxis(Vector3 axis)
    {
        if (axis.sqrMagnitude < 0.0001f)
        {
            return Vector3.right;
        }

        return axis.normalized;
    }

    void NormalizeAxis(ref Vector3 axis, Vector3 fallback)
    {
        if (axis.sqrMagnitude < 0.0001f)
        {
            axis = fallback;
        }
    }
}
