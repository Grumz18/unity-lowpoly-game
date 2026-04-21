using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class PressureButtonMover : MonoBehaviour
{
    public enum MoveDirection
    {
        UpY,
        RightX,
        ForwardZ,
        Custom
    }

    public enum MoveSpace
    {
        World,
        Local
    }

    private enum CycleState
    {
        Idle,
        PressDelay,
        MovingForward,
        HoldAtTop,
        MovingBack
    }

    [Header("Button")]
    public Transform buttonVisual;
    [Min(0.001f)] public float pressDepth = 0.08f;
    [Min(0f)] public float pressSpeed = 0.2f;
    [Min(0f)] public float releaseSpeed = 0.2f;

    [Header("Target Object")]
    public Transform movingObject;
    public MoveDirection direction = MoveDirection.UpY;
    public MoveSpace space = MoveSpace.World;
    public Vector3 customDirection = Vector3.up;
    [Min(0f)] public float moveDistance = 2f;
    [Min(0f)] public float moveForwardSpeed = 1.5f;
    [Min(0f)] public float moveBackwardSpeed = 1.5f;

    [Header("Cycle")]
    [Min(0f)] public float pressStartDelay = 0f;
    public bool autoReturn = true;
    [Min(0f)] public float returnDelay = 0f;
    [Min(0f)] public float triggerReleaseGrace = 0.12f;

    [Header("Activation Filter")]
    public bool onlyPlayer = true;
    public string playerTag = "Player";
    public LayerMask activatorLayers = ~0;

    private readonly HashSet<Collider> activators = new HashSet<Collider>();

    private Vector3 buttonStartLocalPos;
    private Vector3 buttonPressedLocalPos;
    private Vector3 objectStartPos;
    private Vector3 objectEndPos;

    private float buttonAlpha;
    private float moveAlpha;

    private float lastActivationTime = -999f;
    private float stateStartTime = 0f;
    private bool wasPressedLastFrame;
    private CycleState state = CycleState.Idle;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
        }

        CacheTargets();
        ResetRuntimeState();
        ApplyImmediatePose();
    }

    void OnEnable()
    {
        CacheTargets();
        ResetRuntimeState();
        ApplyImmediatePose();
    }

    void OnValidate()
    {
        if (pressDepth < 0.001f) pressDepth = 0.001f;
        if (pressSpeed < 0f) pressSpeed = 0f;
        if (releaseSpeed < 0f) releaseSpeed = 0f;
        if (moveForwardSpeed < 0f) moveForwardSpeed = 0f;
        if (moveBackwardSpeed < 0f) moveBackwardSpeed = 0f;
        if (pressStartDelay < 0f) pressStartDelay = 0f;
        if (returnDelay < 0f) returnDelay = 0f;
        if (triggerReleaseGrace < 0f) triggerReleaseGrace = 0f;

        if (!Application.isPlaying)
        {
            CacheTargets();
            ApplyImmediatePose();
        }
    }

    void Update()
    {
        CleanupInvalidActivators();

        bool pressed = IsPressedNow();
        bool pressedEdge = pressed && !wasPressedLastFrame;
        wasPressedLastFrame = pressed;

        if (state == CycleState.Idle && pressedEdge)
        {
            BeginCycle();
        }

        UpdateCycleState();
        UpdateVisualButton(pressed);
        ApplyImmediatePose();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsActivator(other)) return;

        activators.Add(other);
        lastActivationTime = Time.time;
    }

    void OnTriggerStay(Collider other)
    {
        if (!IsActivator(other)) return;

        activators.Add(other);
        lastActivationTime = Time.time;
    }

    void OnTriggerExit(Collider other)
    {
        if (!activators.Remove(other)) return;

        if (activators.Count == 0)
        {
            lastActivationTime = Time.time;
        }
    }

    [ContextMenu("Cache Start Positions")]
    public void CacheTargets()
    {
        if (buttonVisual == null)
        {
            buttonVisual = transform;
        }

        buttonStartLocalPos = buttonVisual.localPosition;
        buttonPressedLocalPos = buttonStartLocalPos + Vector3.down * pressDepth;

        if (movingObject != null)
        {
            PlatformMover mover = movingObject.GetComponent<PlatformMover>();
            if (mover != null && mover.enabled)
            {
                Debug.LogWarning("PressureButtonMover: movingObject also has enabled PlatformMover. This can cause shaking.", movingObject);
            }

            objectStartPos = movingObject.position;
            objectEndPos = objectStartPos + ResolveMoveAxis() * moveDistance;
        }
    }

    [ContextMenu("Reset To Start")]
    public void ResetToStart()
    {
        ResetRuntimeState();
        ApplyImmediatePose();
    }

    private void ResetRuntimeState()
    {
        activators.Clear();
        buttonAlpha = 0f;
        moveAlpha = 0f;
        lastActivationTime = -999f;
        stateStartTime = Time.time;
        wasPressedLastFrame = false;
        state = CycleState.Idle;
    }

    private bool IsPressedNow()
    {
        bool raw = activators.Count > 0;
        return raw || (Time.time - lastActivationTime <= triggerReleaseGrace);
    }

    private void BeginCycle()
    {
        state = CycleState.PressDelay;
        stateStartTime = Time.time;
    }

    private void UpdateCycleState()
    {
        float dt = Time.deltaTime;

        switch (state)
        {
            case CycleState.Idle:
                moveAlpha = Mathf.MoveTowards(moveAlpha, 0f, GetMoveStep(false) * dt);
                break;

            case CycleState.PressDelay:
                if (Time.time >= stateStartTime + pressStartDelay)
                {
                    state = CycleState.MovingForward;
                    stateStartTime = Time.time;
                }
                break;

            case CycleState.MovingForward:
                moveAlpha = Mathf.MoveTowards(moveAlpha, 1f, GetMoveStep(true) * dt);
                if (moveAlpha >= 0.9999f)
                {
                    moveAlpha = 1f;
                    if (autoReturn)
                    {
                        state = CycleState.HoldAtTop;
                        stateStartTime = Time.time;
                    }
                    else
                    {
                        state = CycleState.Idle;
                    }
                }
                break;

            case CycleState.HoldAtTop:
                if (Time.time >= stateStartTime + returnDelay)
                {
                    state = CycleState.MovingBack;
                    stateStartTime = Time.time;
                }
                break;

            case CycleState.MovingBack:
                moveAlpha = Mathf.MoveTowards(moveAlpha, 0f, GetMoveStep(false) * dt);
                if (moveAlpha <= 0.0001f)
                {
                    moveAlpha = 0f;
                    state = CycleState.Idle;
                }
                break;
        }
    }

    private void UpdateVisualButton(bool pressed)
    {
        bool buttonShouldStayPressed = state != CycleState.Idle || pressed;
        float target = buttonShouldStayPressed ? 1f : 0f;
        float step = (buttonShouldStayPressed ? GetButtonStep(true) : GetButtonStep(false)) * Time.deltaTime;
        buttonAlpha = Mathf.MoveTowards(buttonAlpha, target, step);
    }

    private void CleanupInvalidActivators()
    {
        if (activators.Count == 0) return;

        activators.RemoveWhere(c => c == null || !c.gameObject.activeInHierarchy);
    }

    private void ApplyImmediatePose()
    {
        if (buttonVisual != null)
        {
            buttonVisual.localPosition = Vector3.Lerp(buttonStartLocalPos, buttonPressedLocalPos, buttonAlpha);
        }

        if (movingObject != null)
        {
            movingObject.position = Vector3.Lerp(objectStartPos, objectEndPos, moveAlpha);
        }
    }

    private float GetButtonStep(bool pressing)
    {
        float speed = pressing ? pressSpeed : releaseSpeed;
        if (pressDepth <= 0.0001f) return 1f;

        return speed <= 0f ? 0f : speed / pressDepth;
    }

    private float GetMoveStep(bool forward)
    {
        float speed = forward ? moveForwardSpeed : moveBackwardSpeed;
        if (moveDistance <= 0.0001f) return 1f;

        return speed <= 0f ? 0f : speed / moveDistance;
    }

    private Vector3 ResolveMoveAxis()
    {
        Vector3 axis;

        switch (direction)
        {
            case MoveDirection.RightX:
                axis = Vector3.right;
                break;
            case MoveDirection.ForwardZ:
                axis = Vector3.forward;
                break;
            case MoveDirection.Custom:
                axis = customDirection;
                break;
            default:
                axis = Vector3.up;
                break;
        }

        if (axis.sqrMagnitude < 0.0001f)
        {
            axis = Vector3.up;
        }

        axis = axis.normalized;

        if (space == MoveSpace.Local && movingObject != null)
        {
            axis = movingObject.rotation * axis;
        }

        return axis;
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
