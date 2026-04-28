using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Lives")]
    [Min(1)] public int startLives = 3;
    [Min(0f)] public float damageCooldown = 0.4f;

    [Header("UI")]
    public TMP_Text livesText;
    public GameObject pausePanel;
    public CursorLockMode gameplayCursorLockMode = CursorLockMode.Locked;
    public bool gameplayCursorVisible;

    [Header("Player")]
    public Transform player;

    private int currentLives;
    private bool isPaused;
    private float lastDamageTime = -999f;
    private ProceduralAnimator proceduralAnimator;
    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;
    private bool hasInitialSpawn;
    private Vector3 checkpointPosition;
    private Quaternion checkpointRotation;
    private bool hasCheckpoint;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        ResolvePlayerReference();
        CacheInitialSpawnPoint();
        currentLives = startLives;
        UpdateLivesUI();
        SetPauseState(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TakeDamage(int damage = 1)
    {
        if (Time.unscaledTime - lastDamageTime < damageCooldown)
        {
            return;
        }

        lastDamageTime = Time.unscaledTime;
        currentLives -= Mathf.Abs(damage);
        if (currentLives < 0)
        {
            currentLives = 0;
        }

        UpdateLivesUI();
        PlayDamageFeedback();

        if (currentLives == 0)
        {
            HandleOutOfLives();
        }
    }

    public void TogglePause()
    {
        SetPauseState(!isPaused);
    }

    public void ResumeGame()
    {
        if (isPaused)
        {
            TogglePause();
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        ApplyCursorStateForGameplay();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetPauseState(bool pause)
    {
        isPaused = pause;
        Time.timeScale = isPaused ? 0f : 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }

        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            ApplyCursorStateForGameplay();
        }
    }

    public void SetCheckpoint(Transform checkpointTransform)
    {
        if (checkpointTransform == null)
        {
            return;
        }

        checkpointPosition = checkpointTransform.position;
        checkpointRotation = checkpointTransform.rotation;
        hasCheckpoint = true;
    }

    private void HandleOutOfLives()
    {
        bool respawned = RespawnPlayerAtSavedPoint();
        if (!respawned)
        {
            RestartLevel();
            return;
        }

        currentLives = startLives;
        UpdateLivesUI();
        lastDamageTime = Time.unscaledTime;
    }

    private bool RespawnPlayerAtSavedPoint()
    {
        if (!ResolvePlayerReference())
        {
            return false;
        }

        Vector3 targetPos = hasCheckpoint ? checkpointPosition : initialSpawnPosition;
        Quaternion targetRot = hasCheckpoint ? checkpointRotation : initialSpawnRotation;

        if (!hasCheckpoint && !hasInitialSpawn)
        {
            return false;
        }

        SetPauseState(false);
        TeleportPlayer(targetPos, targetRot);
        return true;
    }

    private void TeleportPlayer(Vector3 targetPos, Quaternion targetRot)
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        PlayerController pc = player.GetComponent<PlayerController>();

        if (cc != null)
        {
            cc.enabled = false;
        }

        player.SetPositionAndRotation(targetPos, targetRot);

        if (pc != null)
        {
            pc.verticalVelocity = 0f;
            pc.moveDirection = Vector3.zero;
        }

        if (cc != null)
        {
            cc.enabled = true;
        }

        if (proceduralAnimator != null)
        {
            proceduralAnimator.ResetDamageFeedback();
        }
    }

    private bool ResolvePlayerReference()
    {
        if (player != null)
        {
            if (proceduralAnimator == null)
            {
                proceduralAnimator = player.GetComponent<ProceduralAnimator>();
                if (proceduralAnimator == null)
                {
                    proceduralAnimator = player.GetComponentInChildren<ProceduralAnimator>();
                }
            }
            return true;
        }

        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc == null)
        {
            return false;
        }

        player = pc.transform;
        if (proceduralAnimator == null)
        {
            proceduralAnimator = player.GetComponent<ProceduralAnimator>();
            if (proceduralAnimator == null)
            {
                proceduralAnimator = player.GetComponentInChildren<ProceduralAnimator>();
            }
        }
        return true;
    }

    private void CacheInitialSpawnPoint()
    {
        if (!ResolvePlayerReference())
        {
            return;
        }

        initialSpawnPosition = player.position;
        initialSpawnRotation = player.rotation;
        hasInitialSpawn = true;
    }

    private void ApplyCursorStateForGameplay()
    {
        Cursor.lockState = gameplayCursorLockMode;
        Cursor.visible = gameplayCursorVisible;
    }

    private void UpdateLivesUI()
    {
        if (livesText != null)
        {
            livesText.text = $"Lives: {currentLives}";
        }
    }

    private void PlayDamageFeedback()
    {
        if (proceduralAnimator == null)
        {
            ResolvePlayerReference();
        }

        if (proceduralAnimator != null)
        {
            proceduralAnimator.PlayDamageFeedback();
        }
    }
}
