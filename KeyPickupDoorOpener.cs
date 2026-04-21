using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class KeyPickupDoorOpener : MonoBehaviour
{
    public enum DoorOpenMode
    {
        RotateLocal,
        MoveLocal,
        MoveWorld
    }

    [Header("Target Door")]
    public Transform doorTarget;
    public DoorOpenMode doorOpenMode = DoorOpenMode.RotateLocal;
    [Tooltip("Local rotation offset (Euler) applied when opening.")]
    public Vector3 doorOpenOffset = new Vector3(0f, 0f, 0f);
    [Tooltip("Position offset applied when opening. Can be used together with rotation to compensate center pivot.")]
    public Vector3 doorOpenPositionOffset = Vector3.zero;
    [Tooltip("If true, open motion starts from door transform at pickup moment. If false, from cached start transform.")]
    public bool useCurrentDoorTransformAsStart = true;
    [Min(0.01f)] public float doorOpenDuration = 1.2f;
    public AnimationCurve doorOpenEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Key Idle Motion")]
    public float spinSpeed = 120f;
    public Vector3 spinAxis = Vector3.up;
    public float bobAmount = 0.08f;
    public float bobSpeed = 2f;

    [Header("Activation Filter")]
    public bool onlyPlayer = true;
    public string playerTag = "Player";
    public LayerMask activatorLayers = ~0;

    [Header("Pickup Behavior")]
    public bool disableColliderOnPickup = true;
    public bool hideKeyOnPickup = true;
    public bool destroyKeyAfterOpen = false;
    [Min(0f)] public float destroyDelay = 0f;

    private Collider keyCollider;
    private Renderer[] keyRenderers;
    private Vector3 startLocalPos;
    private Quaternion doorStartLocalRot;
    private Vector3 doorStartLocalPos;
    private Vector3 doorStartWorldPos;
    private bool picked;

    void Awake()
    {
        keyCollider = GetComponent<Collider>();
        keyCollider.isTrigger = true;

        keyRenderers = GetComponentsInChildren<Renderer>(true);
        startLocalPos = transform.localPosition;

        if (doorTarget != null)
        {
            doorStartLocalRot = doorTarget.localRotation;
            doorStartLocalPos = doorTarget.localPosition;
            doorStartWorldPos = doorTarget.position;
        }
    }

    void Update()
    {
        if (picked)
        {
            return;
        }

        if (spinAxis.sqrMagnitude > 0.0001f && spinSpeed != 0f)
        {
            transform.Rotate(spinAxis.normalized, spinSpeed * Time.deltaTime, Space.Self);
        }

        if (bobAmount > 0f && bobSpeed > 0f)
        {
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            transform.localPosition = startLocalPos + Vector3.up * bob;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (picked || !IsActivator(other))
        {
            return;
        }

        picked = true;
        OnPickup();
    }

    private void OnPickup()
    {
        if (disableColliderOnPickup && keyCollider != null)
        {
            keyCollider.enabled = false;
        }

        if (hideKeyOnPickup)
        {
            for (int i = 0; i < keyRenderers.Length; i++)
            {
                keyRenderers[i].enabled = false;
            }
        }

        if (doorTarget != null)
        {
            StartCoroutine(OpenDoorRoutine());
        }
        else if (destroyKeyAfterOpen)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    private IEnumerator OpenDoorRoutine()
    {
        float duration = Mathf.Max(0.01f, doorOpenDuration);
        float t = 0f;

        Quaternion fromRot = useCurrentDoorTransformAsStart ? doorTarget.localRotation : doorStartLocalRot;
        Quaternion toRot = fromRot * Quaternion.Euler(doorOpenOffset);
        Vector3 fromLocalPos = useCurrentDoorTransformAsStart ? doorTarget.localPosition : doorStartLocalPos;
        Vector3 toLocalPos = fromLocalPos + doorOpenPositionOffset;
        Vector3 fromWorldPos = useCurrentDoorTransformAsStart ? doorTarget.position : doorStartWorldPos;
        Vector3 toWorldPos = fromWorldPos + doorOpenPositionOffset;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float e = doorOpenEase != null ? doorOpenEase.Evaluate(p) : p;

            switch (doorOpenMode)
            {
                case DoorOpenMode.RotateLocal:
                    doorTarget.localRotation = Quaternion.Slerp(fromRot, toRot, e);
                    doorTarget.localPosition = Vector3.Lerp(fromLocalPos, toLocalPos, e);
                    break;
                case DoorOpenMode.MoveLocal:
                    doorTarget.localPosition = Vector3.Lerp(fromLocalPos, toLocalPos, e);
                    break;
                case DoorOpenMode.MoveWorld:
                    doorTarget.position = Vector3.Lerp(fromWorldPos, toWorldPos, e);
                    break;
            }

            yield return null;
        }

        if (destroyKeyAfterOpen)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    private bool IsActivator(Collider other)
    {
        if (((1 << other.gameObject.layer) & activatorLayers.value) == 0)
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

        if (other.GetComponentInParent<PlayerController>() != null)
        {
            return true;
        }

        return false;
    }
}
