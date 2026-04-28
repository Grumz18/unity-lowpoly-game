using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float rotationSpeed = 10f;

    [Header("Jump")]
    public float jumpForce = 8f;
    public float gravity = -20f;

    [Header("Ladder")]
    public float ladderClimbSpeed = 3.5f;
    public float ladderContactGrace = 0.15f;
    public bool climbOnlyOnForward = true;

    [Header("Camera")]
    public Transform cameraTransform;

    [HideInInspector] public Vector3 moveDirection;
    [HideInInspector] public float currentSpeed;
    [HideInInspector] public bool isGrounded;
    [HideInInspector] public bool isRunning;
    [HideInInspector] public bool isJumping;
    [HideInInspector] public float verticalVelocity;

    private CharacterController cc;
    private PlatformMover platformCandidate;
    private PlatformMover attachedPlatform;
    private Transform carrierCandidate;
    private Transform attachedCarrier;
    private Vector3 attachedCarrierLastPos;
    private bool attachedCarrierHasLastPos;
    private LadderZone ladderCandidate;
    private LadderZone activeLadder;
    private float lastLadderContactTime = -999f;
    private bool isClimbing;

    void Start()
    {
        cc = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Reset per-frame candidate. It will be filled during cc.Move by OnControllerColliderHit.
        platformCandidate = null;
        carrierCandidate = null;
        ladderCandidate = null;

        isGrounded = cc.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
            isJumping = false;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        isRunning = Input.GetKey(KeyCode.LeftShift);

        Vector3 camForward;
        Vector3 camRight;

        if (cameraTransform != null)
        {
            camForward = cameraTransform.forward;
            camRight = cameraTransform.right;
        }
        else
        {
            camForward = transform.forward;
            camRight = transform.right;
        }

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 inputDir = camForward * v + camRight * h;
        float inputMagnitude = Mathf.Clamp01(inputDir.magnitude);
        inputDir = inputMagnitude > 0f ? inputDir.normalized : Vector3.zero;

        if (inputMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(inputDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        float speed = isRunning ? runSpeed : moveSpeed;
        moveDirection = inputDir * speed * inputMagnitude;
        currentSpeed = inputMagnitude * (isRunning ? 1f : 0.5f);

        bool jumpPressed = Input.GetButtonDown("Jump");
        if (jumpPressed && isGrounded && !isClimbing)
        {
            verticalVelocity = jumpForce;
            isJumping = true;
            DetachFromPlatform();
        }

        UpdateLadderState(v);

        if (!isClimbing)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        else
        {
            verticalVelocity = 0f;
            isJumping = false;
            DetachFromPlatform();
        }

        Vector3 finalMove = moveDirection + Vector3.up * verticalVelocity;
        if (isClimbing)
        {
            Vector3 climbAxis = GetLadderAxis();
            float climbInput = climbOnlyOnForward ? Mathf.Max(0f, v) : v;
            finalMove = climbAxis * (climbInput * GetLadderSpeed());
            currentSpeed = Mathf.Abs(climbInput) * 0.5f;
        }

        Vector3 platformDelta = Vector3.zero;
        if (attachedPlatform != null && isGrounded)
        {
            platformDelta = attachedPlatform.DeltaPosition;
        }
        else if (attachedCarrier != null && isGrounded)
        {
            Vector3 currentPos = attachedCarrier.position;
            if (attachedCarrierHasLastPos)
            {
                platformDelta = currentPos - attachedCarrierLastPos;
            }
            attachedCarrierLastPos = currentPos;
            attachedCarrierHasLastPos = true;
        }

        cc.Move(platformDelta + finalMove * Time.deltaTime);

        UpdatePlatformAttachment();
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        DamageZone damageZone = hit.collider.GetComponentInParent<DamageZone>();
        if (damageZone != null)
        {
            damageZone.TryApplyDamage(gameObject);
        }

        InstantTripleDamageZone instantTripleDamageZone = hit.collider.GetComponentInParent<InstantTripleDamageZone>();
        if (instantTripleDamageZone != null)
        {
            instantTripleDamageZone.TryApplyDamage(gameObject);
        }

        LadderZone ladder = hit.collider.GetComponentInParent<LadderZone>();
        if (ladder != null)
        {
            activeLadder = ladder;
            ladderCandidate = ladder;
            lastLadderContactTime = Time.time;
        }

        // Only surfaces under feet should attach the player.
        if (hit.normal.y < 0.35f)
        {
            return;
        }

        carrierCandidate = hit.collider.transform;

        PlatformMover mover = hit.collider.GetComponentInParent<PlatformMover>();
        if (mover != null)
        {
            platformCandidate = mover;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        LadderZone ladder = other.GetComponentInParent<LadderZone>();
        if (ladder == null)
        {
            return;
        }

        activeLadder = ladder;
        ladderCandidate = ladder;
        lastLadderContactTime = Time.time;
    }

    void OnTriggerStay(Collider other)
    {
        LadderZone ladder = other.GetComponentInParent<LadderZone>();
        if (ladder == null)
        {
            return;
        }

        activeLadder = ladder;
        ladderCandidate = ladder;
        lastLadderContactTime = Time.time;
    }

    void OnTriggerExit(Collider other)
    {
        LadderZone ladder = other.GetComponentInParent<LadderZone>();
        if (ladder == null)
        {
            return;
        }

        if (activeLadder == ladder)
        {
            // Keep by grace timer for a short moment to avoid jitter at trigger borders.
            lastLadderContactTime = Time.time;
        }
    }

    void UpdatePlatformAttachment()
    {
        if (isGrounded && platformCandidate != null)
        {
            AttachToPlatform(platformCandidate);
        }
        else if (isGrounded && carrierCandidate != null)
        {
            AttachToCarrier(carrierCandidate);
        }
        else if (isGrounded && (attachedPlatform != null || attachedCarrier != null))
        {
            // Keep previous attachment for one or more frames if contact callbacks are intermittent.
            // This prevents jitter on vertically moving platforms.
            return;
        }
        else
        {
            DetachFromPlatform();
        }
    }

    void AttachToPlatform(PlatformMover platform)
    {
        if (platform == null || attachedPlatform == platform)
        {
            return;
        }

        attachedPlatform = platform;
        attachedCarrier = null;
        attachedCarrierHasLastPos = false;
    }

    void DetachFromPlatform()
    {
        if (attachedPlatform == null && attachedCarrier == null)
        {
            return;
        }

        attachedPlatform = null;
        attachedCarrier = null;
        attachedCarrierHasLastPos = false;
    }

    void AttachToCarrier(Transform carrier)
    {
        if (carrier == null || attachedCarrier == carrier)
        {
            return;
        }

        attachedPlatform = null;
        attachedCarrier = carrier;
        attachedCarrierLastPos = carrier.position;
        attachedCarrierHasLastPos = true;
    }

    void UpdateLadderState(float verticalInput)
    {
        if (ladderCandidate != null)
        {
            activeLadder = ladderCandidate;
        }

        bool ladderAvailable = activeLadder != null && (Time.time - lastLadderContactTime <= ladderContactGrace);
        bool wantsClimb = climbOnlyOnForward ? verticalInput > 0.1f : Mathf.Abs(verticalInput) > 0.1f;

        if (ladderAvailable && wantsClimb)
        {
            isClimbing = true;
            return;
        }

        if (!ladderAvailable || (climbOnlyOnForward && verticalInput <= 0.05f))
        {
            isClimbing = false;
        }
    }

    Vector3 GetLadderAxis()
    {
        if (activeLadder == null)
        {
            return Vector3.up;
        }

        Vector3 axis = activeLadder.GetWorldClimbAxis();
        if (axis.sqrMagnitude < 0.0001f)
        {
            return Vector3.up;
        }

        return axis.normalized;
    }

    float GetLadderSpeed()
    {
        if (activeLadder == null)
        {
            return ladderClimbSpeed;
        }

        return ladderClimbSpeed * Mathf.Max(0.01f, activeLadder.speedMultiplier);
    }
}
