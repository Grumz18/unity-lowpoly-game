using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class InstantTripleDamageZone : MonoBehaviour
{
    [Min(1)] public int damage = 3;
    public bool onlyPlayer = true;
    public string playerTag = "Player";

    void OnTriggerEnter(Collider other)
    {
        TryApplyDamage(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        TryApplyDamage(collision.gameObject);
    }

    public void TryApplyDamage(GameObject other)
    {
        if (!IsTarget(other))
        {
            return;
        }

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.TakeDamage(damage);
        }
    }

    private bool IsTarget(GameObject other)
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
