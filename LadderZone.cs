using UnityEngine;

[DisallowMultipleComponent]
public class LadderZone : MonoBehaviour
{
    [Tooltip("If true, axis is transformed by ladder rotation. If false, world axis is used directly.")]
    public bool useLocalAxis = true;

    [Tooltip("Climb axis. Usually (0,1,0).")]
    public Vector3 climbAxis = Vector3.up;

    [Tooltip("Optional speed multiplier for this specific ladder.")]
    [Min(0.01f)] public float speedMultiplier = 1f;

    public Vector3 GetWorldClimbAxis()
    {
        Vector3 axis = climbAxis.sqrMagnitude < 0.0001f ? Vector3.up : climbAxis.normalized;
        return useLocalAxis ? transform.TransformDirection(axis).normalized : axis;
    }
}

