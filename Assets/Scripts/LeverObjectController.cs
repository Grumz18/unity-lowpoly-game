using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class LeverObjectController : MonoBehaviour
{
    [Header("Target Object")]
    public Transform targetObject;
    public bool usePositionOffset;
    public Vector3 activatedPositionOffset;
    public bool useRotationOffset = true;
    public Vector3 activatedRotationOffset = new Vector3(90f, 0f, 0f);
    public bool useScaleMultiplier;
    public Vector3 activatedScaleMultiplier = Vector3.one;
    [Min(0.01f)] public float activationDuration = 1.2f;

    [Header("Lever Visual")]
    public Transform leverVisual;
    public Vector3 leverActivatedRotationOffset = new Vector3(-45f, 0f, 0f);

    [Header("Activation")]
    public bool onlyPlayer = true;
    public string playerTag = "Player";

    private Vector3 targetStartPosition;
    private Vector3 targetActivatedPosition;
    private Quaternion targetStartRotation;
    private Quaternion targetActivatedRotation;
    private Vector3 targetStartScale;
    private Vector3 targetActivatedScale;

    private Quaternion leverStartRotation;
    private Quaternion leverActivatedRotation;

    private float progress;
    private float targetProgress;
    private bool isMoving;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
        }

        if (leverVisual == null)
        {
            leverVisual = transform;
        }

        CacheStartPose();
        ApplyPose(0f);
    }

    void OnValidate()
    {
        if (activationDuration < 0.01f)
        {
            activationDuration = 0.01f;
        }
    }

    void Update()
    {
        if (!isMoving)
        {
            return;
        }

        progress = Mathf.MoveTowards(progress, targetProgress, Time.deltaTime / activationDuration);
        ApplyPose(progress);

        if (Mathf.Approximately(progress, targetProgress))
        {
            progress = targetProgress;
            isMoving = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsActivator(other.gameObject))
        {
            return;
        }

        Activate();
    }

    public void Activate()
    {
        if (isMoving)
        {
            return;
        }

        targetProgress = progress < 0.5f ? 1f : 0f;
        isMoving = true;
    }

    [ContextMenu("Reset Lever And Target")]
    public void ResetLeverAndTarget()
    {
        isMoving = false;
        progress = 0f;
        targetProgress = 0f;
        ApplyPose(0f);
    }

    private void CacheStartPose()
    {
        if (targetObject != null)
        {
            targetStartPosition = targetObject.localPosition;
            targetActivatedPosition = targetStartPosition + activatedPositionOffset;
            targetStartRotation = targetObject.localRotation;
            targetActivatedRotation = targetStartRotation * Quaternion.Euler(activatedRotationOffset);
            targetStartScale = targetObject.localScale;
            targetActivatedScale = new Vector3(
                targetStartScale.x * activatedScaleMultiplier.x,
                targetStartScale.y * activatedScaleMultiplier.y,
                targetStartScale.z * activatedScaleMultiplier.z);
        }

        if (leverVisual != null)
        {
            leverStartRotation = leverVisual.localRotation;
            leverActivatedRotation = leverStartRotation * Quaternion.Euler(leverActivatedRotationOffset);
        }
    }

    private void ApplyPose(float t)
    {
        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        if (targetObject != null)
        {
            if (usePositionOffset)
            {
                targetObject.localPosition = Vector3.Lerp(targetStartPosition, targetActivatedPosition, smoothT);
            }

            if (useRotationOffset)
            {
                targetObject.localRotation = Quaternion.Slerp(targetStartRotation, targetActivatedRotation, smoothT);
            }

            if (useScaleMultiplier)
            {
                targetObject.localScale = Vector3.Lerp(targetStartScale, targetActivatedScale, smoothT);
            }
        }

        if (leverVisual != null)
        {
            leverVisual.localRotation = Quaternion.Slerp(leverStartRotation, leverActivatedRotation, smoothT);
        }
    }

    private bool IsActivator(GameObject other)
    {
        if (other == null)
        {
            return false;
        }

        if (!onlyPlayer)
        {
            return true;
        }

        if (other.CompareTag(playerTag))
        {
            return true;
        }

        return other.GetComponentInParent<PlayerController>() != null;
    }
}
