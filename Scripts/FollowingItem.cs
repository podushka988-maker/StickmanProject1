using UnityEngine;
public class FollowingItem : MonoBehaviour
{
    [Header("Цель")]
    [Tooltip("Объект, за которым следует камера.")]
    [SerializeField] private Transform target;

    [Header("Следование")]
    [Tooltip("Скорость догона. Больше — резче, меньше — плавнее.")]
    [SerializeField] private float smoothSpeed = 5f;

    [Tooltip("Смещение камеры относительно игрока.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}
