using UnityEngine;

/// <summary>
/// Third-person камера: вращение мышью вокруг персонажа,
/// плавное следование, коллизия с геометрией.
/// Вешается на Main Camera.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Цель")]
    [Tooltip("Перетащи сюда GameObject персонажа")]
    public Transform target;

    [Tooltip("Смещение точки, на которую смотрит камера (выше центра)")]
    public Vector3 targetOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Дистанция")]
    public float distance = 5f;
    public float minDistance = 1.5f;
    public float maxDistance = 10f;
    public float scrollSpeed = 2f;

    [Header("Вращение мышью")]
    public float mouseSensitivity = 3f;
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 60f;

    [Header("Плавность")]
    [Tooltip("Скорость следования камеры (больше = быстрее)")]
    public float followSmoothness = 10f;

    [Header("Коллизия")]
    [Tooltip("Камера не проходит сквозь стены")]
    public float collisionRadius = 0.3f;
    public LayerMask collisionLayers = ~0; // всё

    // Внутреннее состояние
    private float yaw;   // горизонтальный угол
    private float pitch; // вертикальный угол

    void Start()
    {
        if (target == null)
        {
            // Пытаемся найти персонажа по тегу или PlayerController
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null) target = pc.transform;
        }

        // Начальные углы из текущей позиции камеры
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // --- Ввод мыши ---
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        // --- Скролл: изменение дистанции ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * scrollSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // --- Позиция камеры ---
        Vector3 lookAt = target.position + targetOffset;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = lookAt - rotation * Vector3.forward * distance;

        // --- Коллизия: не даём камере залезть в стены ---
        Vector3 dir = desiredPos - lookAt;
        float desiredDist = dir.magnitude;

        if (Physics.SphereCast(lookAt, collisionRadius, dir.normalized,
            out RaycastHit hit, desiredDist, collisionLayers))
        {
            desiredPos = lookAt + dir.normalized * (hit.distance - collisionRadius * 0.5f);
        }

        // --- Плавное движение ---
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSmoothness * Time.deltaTime);
        transform.LookAt(lookAt);
    }
}
