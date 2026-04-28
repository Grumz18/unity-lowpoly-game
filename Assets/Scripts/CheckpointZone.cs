using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class CheckpointZone : MonoBehaviour
{
    public Transform respawnPoint;
    public bool onlyPlayer = true;
    public string playerTag = "Player";
    [Header("Visual Feedback")]
    public Transform scaleTarget;
    [Range(0.1f, 1f)] public float activatedScaleMultiplier = 0.9f;

    private Vector3 initialScale;
    private bool isActivated;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
        }

        if (scaleTarget == null)
        {
            scaleTarget = transform;
        }

        initialScale = scaleTarget.localScale;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsTarget(other.gameObject))
        {
            return;
        }

        if (GameUIManager.Instance == null)
        {
            return;
        }

        Transform targetPoint = respawnPoint != null ? respawnPoint : transform;
        GameUIManager.Instance.SetCheckpoint(targetPoint);
        ApplyActivatedVisual();
    }

    private bool IsTarget(GameObject other)
    {
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

    private void ApplyActivatedVisual()
    {
        if (isActivated || scaleTarget == null)
        {
            return;
        }

        float multiplier = Mathf.Clamp(activatedScaleMultiplier, 0.1f, 1f);
        scaleTarget.localScale = initialScale * multiplier;
        isActivated = true;
    }
}
