using UnityEngine;

[DisallowMultipleComponent]
public class PlatformMover : MonoBehaviour
{
    public enum DirectionMode
    {
        HorizontalX,
        VerticalY,
        DepthZ,
        Custom
    }

    public enum SpaceMode
    {
        World,
        Local
    }

    public enum MotionType
    {
        Wave,
        Triangle,
        PingPong,
        Saw,
        Step
    }

    [Header("Direction")]
    public DirectionMode direction = DirectionMode.HorizontalX;
    public SpaceMode space = SpaceMode.World;
    public Vector3 customDirection = Vector3.right;

    [Header("Motion")]
    public MotionType motion = MotionType.Wave;
    [Min(0.01f)] public float period = 4f;
    [Min(0f)] public float randomPeriod = 0f;
    public float phaseOffset = 0f;
    [Min(0f)] public float randomTimeOffset = 0f;
    public float amplitude = 1f;
    [Min(0f)] public float randomAmplitude = 0f;
    [Min(0f)] public float speed = 1f;

    [Header("Options")]
    public bool playOnStart = true;
    public bool useUnscaledTime = false;
    public bool reRollRandomOnEnable = false;
    public Vector3 DeltaPosition { get; private set; }

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float runtimePeriod;
    private float runtimeAmplitude;
    private float runtimeTimeOffset;
    private bool initialized;
    private Vector3 lastFramePosition;
    private float motionClock;

    void Awake()
    {
        Initialize();
    }

    void OnEnable()
    {
        if (!initialized)
        {
            Initialize();
            return;
        }

        if (reRollRandomOnEnable)
        {
            RollRuntimeValues();
        }

        motionClock = 0f;
        ApplyInstantPose();
        lastFramePosition = transform.position;
        DeltaPosition = Vector3.zero;
    }

    void OnValidate()
    {
        if (period < 0.01f) period = 0.01f;
        if (speed < 0f) speed = 0f;
        if (randomPeriod < 0f) randomPeriod = 0f;
        if (randomTimeOffset < 0f) randomTimeOffset = 0f;
        if (randomAmplitude < 0f) randomAmplitude = 0f;

        if (!Application.isPlaying)
        {
            // Do not auto-apply animated offset in edit mode to avoid position drift.
            runtimePeriod = Mathf.Max(0.01f, period);
            runtimeAmplitude = amplitude;
            runtimeTimeOffset = phaseOffset;
        }
    }

    void Update()
    {
        Vector3 before = transform.position;

        if (!playOnStart)
        {
            DeltaPosition = Vector3.zero;
            lastFramePosition = before;
            return;
        }

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        motionClock += dt;
        float value = EvaluateMotion(motionClock * speed + runtimeTimeOffset, runtimePeriod, motion);
        Vector3 axis = ResolveAxis();
        transform.position = startPosition + axis * (runtimeAmplitude * value);
        DeltaPosition = transform.position - before;
        lastFramePosition = transform.position;
    }

    [ContextMenu("Recenter To Current Position")]
    public void RecenterToCurrentPosition()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        motionClock = 0f;
        lastFramePosition = transform.position;
        DeltaPosition = Vector3.zero;
    }

    [ContextMenu("Re-Roll Random")]
    public void ReRollRandom()
    {
        RollRuntimeValues();
        ApplyInstantPose();
    }

    [ContextMenu("Reset To Start")]
    public void ResetToStart()
    {
        transform.position = startPosition;
        motionClock = 0f;
        lastFramePosition = transform.position;
        DeltaPosition = Vector3.zero;
    }

    private void Initialize()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        RollRuntimeValues();
        initialized = true;
        motionClock = 0f;
        ApplyInstantPose();
        lastFramePosition = transform.position;
        DeltaPosition = Vector3.zero;
    }

    private void RollRuntimeValues()
    {
        runtimePeriod = Mathf.Max(0.01f, period + Random.Range(-randomPeriod, randomPeriod));
        runtimeAmplitude = amplitude + Random.Range(-randomAmplitude, randomAmplitude);
        runtimeTimeOffset = phaseOffset + Random.Range(-randomTimeOffset, randomTimeOffset);
    }

    private void ApplyInstantPose()
    {
        float value = EvaluateMotion(motionClock * speed + runtimeTimeOffset, Mathf.Max(0.01f, runtimePeriod), motion);
        transform.position = startPosition + ResolveAxis() * (runtimeAmplitude * value);
        DeltaPosition = transform.position - lastFramePosition;
        lastFramePosition = transform.position;
    }

    private Vector3 ResolveAxis()
    {
        Vector3 axis;

        switch (direction)
        {
            case DirectionMode.HorizontalX:
                axis = Vector3.right;
                break;
            case DirectionMode.VerticalY:
                axis = Vector3.up;
                break;
            case DirectionMode.DepthZ:
                axis = Vector3.forward;
                break;
            default:
                axis = customDirection;
                break;
        }

        if (axis.sqrMagnitude < 0.0001f)
        {
            axis = Vector3.right;
        }

        axis = axis.normalized;

        if (space == SpaceMode.Local)
        {
            axis = startRotation * axis;
        }

        return axis;
    }

    private static float EvaluateMotion(float timeValue, float cyclePeriod, MotionType motionType)
    {
        float cycles = timeValue / Mathf.Max(0.01f, cyclePeriod);
        float phase01 = cycles - Mathf.Floor(cycles);

        switch (motionType)
        {
            case MotionType.Wave:
                return Mathf.Sin(phase01 * Mathf.PI * 2f);

            case MotionType.Triangle:
                return 1f - 4f * Mathf.Abs(phase01 - 0.5f);

            case MotionType.PingPong:
            {
                float ping = Mathf.PingPong(phase01 * 2f, 1f);
                return Mathf.SmoothStep(-1f, 1f, ping);
            }

            case MotionType.Saw:
                return phase01 * 2f - 1f;

            case MotionType.Step:
                return phase01 < 0.5f ? 1f : -1f;

            default:
                return 0f;
        }
    }
}
