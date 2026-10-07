using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(BoxCollider2D))] // Теперь скрипт сам добавит коллайдер игроку!
public class LaserWeapon : MonoBehaviour
{
    [Header("Настройки статического луча")]
    [Tooltip("Длина лазера. Просто меняй эту цифру, чтобы растянуть его от стенки до стенки!")]
    [SerializeField] private float laserLength = 5f;

    [Tooltip("Толщина триггера урона лазера.")]
    [SerializeField] private float laserWidth = 0.2f;

    private LineRenderer lineRenderer;
    private BoxCollider2D boxCollider;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

        // Делаем коллайдер триггером, чтобы игрок не врезался в него, а проходил насквозь и умирал
        boxCollider.isTrigger = true;
    }

    private void Update()
    {
        // Начало — строго в центре белого куба
        Vector3 localStart = Vector3.zero;

        // Конец — идет строго вперед по красной стрелочке (ось X) на указанную длину
        Vector3 localEnd = new Vector3(laserLength, 0f, 0f);

        // 1. Передаем локальные точки в Line Renderer
        lineRenderer.SetPosition(0, localStart);
        lineRenderer.SetPosition(1, localEnd);

        // 2. АВТОМАТИЧЕСКИЙ ФИКС КОЛЛАЙДЕРА: Растягиваем его ровно под длину лазера
        if (boxCollider != null)
        {
            // Размер коллайдера: длина по оси X, толщина по оси Y
            boxCollider.size = new Vector2(laserLength, laserWidth);

            // Сдвигаем центр коллайдера вперед, чтобы он шел ровно вдоль линии лазера
            boxCollider.offset = new Vector2(laserLength / 2f, 0f);
        }
    }
}