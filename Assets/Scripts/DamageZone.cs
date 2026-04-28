using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageZone : MonoBehaviour
{
    [Min(1)] public int damage = 1;
    public bool onlyPlayer = true;
    public string playerTag = "Player";
    public bool damageOnStay = true;
    [Min(0f)] public float damageInterval = 1f;

    private readonly Dictionary<int, float> nextDamageTimeByTarget = new Dictionary<int, float>();

    void OnTriggerEnter(Collider other)
    {
        TryApplyDamage(other.gameObject);
    }

    void OnTriggerStay(Collider other)
    {
        if (!damageOnStay)
        {
            return;
        }

        TryApplyDamage(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        TryApplyDamage(collision.gameObject);
    }

    void OnCollisionStay(Collision collision)
    {
        if (!damageOnStay)
        {
            return;
        }

        TryApplyDamage(collision.gameObject);
    }

    void OnTriggerExit(Collider other)
    {
        ClearCooldownForTarget(other.gameObject);
    }

    void OnCollisionExit(Collision collision)
    {
        ClearCooldownForTarget(collision.gameObject);
    }

    public void TryApplyDamage(GameObject other)
    {
        GameObject target = ResolveTargetObject(other);
        if (!IsTarget(target))
        {
            return;
        }

        int targetId = target.GetInstanceID();
        float now = Time.time;
        if (nextDamageTimeByTarget.TryGetValue(targetId, out float nextAllowedTime) && now < nextAllowedTime)
        {
            return;
        }

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.TakeDamage(damage);
            nextDamageTimeByTarget[targetId] = now + damageInterval;
        }
    }

    private GameObject ResolveTargetObject(GameObject other)
    {
        if (other == null)
        {
            return null;
        }

        PlayerController pc = other.GetComponentInParent<PlayerController>();
        return pc != null ? pc.gameObject : other;
    }

    private void ClearCooldownForTarget(GameObject other)
    {
        GameObject target = ResolveTargetObject(other);
        if (target == null)
        {
            return;
        }

        nextDamageTimeByTarget.Remove(target.GetInstanceID());
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
