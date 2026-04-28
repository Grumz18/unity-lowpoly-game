using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class RestartWorldOnTouch : MonoBehaviour
{
    [Header("Activation Filter")]
    public bool onlyPlayer = true;
    public string playerTag = "Player";
    public LayerMask activatorLayers = ~0;

    private bool isReloading;

    void OnTriggerEnter(Collider other)
    {
        TryRestart(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        TryRestart(collision.gameObject);
    }

    private void TryRestart(GameObject other)
    {
        if (isReloading || !IsActivator(other))
        {
            return;
        }

        isReloading = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private bool IsActivator(GameObject other)
    {
        if (other == null)
        {
            return false;
        }

        if (((1 << other.layer) & activatorLayers.value) == 0)
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
