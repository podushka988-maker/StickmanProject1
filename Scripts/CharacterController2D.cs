using UnityEngine;
using UnityEngine.Events;
public class CharacterController2D : MonoBehaviour
{
    [Header("Настройки прыжка")]
    [Tooltip("Сила, прикладываемая при прыжке.")]
    [SerializeField] private float m_JumpForce = 400f;

    [Header("Настройки движения")]
    [Tooltip("Скорость сглаживания движения. Меньше — резче, больше — плавнее.")]
    [Range(0, .3f)][SerializeField] private float m_MovementSmoothing = .05f;

    [Tooltip("Может ли персонаж управляться в воздухе.")]
    [SerializeField] private bool m_AirControl = false;

    [Tooltip("Слои, которые считаются землей.")]
    [SerializeField] private LayerMask m_WhatIsGround;

    [Tooltip("Точка, откуда проверяется наличие земли под ногами.")]
    [SerializeField] private Transform m_GroundCheck;

    [Header("События")]
    [Space]
    public UnityEvent OnLandEvent;

    private const float k_GroundedRadius = .15f; // Крохотный радиус, чтобы не задевать лишнее
    private bool m_Grounded; // Находится ли персонаж на земле
    private Rigidbody2D m_Rigidbody2D;
    private bool m_FacingRight = true; // В какую сторону смотрит персонаж
    private Vector3 m_Velocity = Vector3.zero;

    // Бронебойный предохранитель: был ли уже совершен прыжок в воздухе
    private bool m_HasJumpedInAir = false;

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();
        if (OnLandEvent == null)
            OnLandEvent = new UnityEvent();

        m_Rigidbody2D.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        bool wasGrounded = m_Grounded;
        m_Grounded = false;

        // Ищем коллайдеры в ногах
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            m_GroundCheck.position,
            k_GroundedRadius,
            m_WhatIsGround
        );

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject &&
                colliders[i].transform.root != transform.root &&
                !colliders[i].isTrigger)
            {
                m_Grounded = true;
                m_HasJumpedInAir = false; // Сбрасываем предохранитель, мы точно коснулись твердой земли!

                if (!wasGrounded)
                    OnLandEvent.Invoke();

                break;
            }
        }
    }

    /// <summary>
    /// Основной метод движения. Вызывается из Update() в PlayerMovement.
    /// </summary>
    public void Move(float move, bool jump)
    {
        if (m_Grounded || m_AirControl)
        {
            Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.linearVelocity.y);
            m_Rigidbody2D.linearVelocity = Vector3.SmoothDamp(
                m_Rigidbody2D.linearVelocity,
                targetVelocity,
                ref m_Velocity,
                m_MovementSmoothing
            );

            if (move > 0 && !m_FacingRight) Flip();
            else if (move < 0 && m_FacingRight) Flip();
        }

        // Прыгаем, только если мы на земле И еще не тратили прыжок в воздухе
        if (jump && m_Grounded && !m_HasJumpedInAir)
        {
            m_Grounded = false;
            m_HasJumpedInAir = true; // Мгновенно сжигаем попытку прыжка до приземления

            // Сбрасываем вертикальную скорость для стабильного толчка
            m_Rigidbody2D.linearVelocity = new Vector2(m_Rigidbody2D.linearVelocity.x, 0f);

            // Толкаем вверх
            m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
        }
    }

    private void Flip()
    {
        m_FacingRight = !m_FacingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        if (m_GroundCheck != null)
        {
            Gizmos.color = Color.red; // Красный кружок в ногах
            Gizmos.DrawWireSphere(m_GroundCheck.position, k_GroundedRadius);
        }
    }
}
