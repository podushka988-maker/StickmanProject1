using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private CharacterController2D controller;

    [Header("Настройки движения")]
    [SerializeField] private float runSpeed = 30f;

    [Header("Звуки игрока")]
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private float footstepInterval = 0.4f;

    [Header("МОБИЛЬНОЕ УПРАВЛЕНИЕ")]
    [SerializeField] private MobileButton mobileLeftButton;
    [SerializeField] private MobileButton mobileRightButton;
    [SerializeField] private MobileButton mobileJumpButton;

    private float horizontalMove = 0f;
    private bool jump = false;
    private float footstepTimer = 0f;

    private Animator anim;
    private bool isJumpingInAir = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>();
        }
    }

    private void Start()
    {
        Debug.Log($"AudioManager: {AudioManager.Instance != null}");
        Debug.Log($"Footstep Sound: {footstepSound != null}");
        Debug.Log($"Jump Sound: {jumpSound != null}");
    }

    private void Update()
    {
        // 1. Проверяем нажатия на ПК (защищаем код от null, если клавиатуры нет)
        bool pcLeft = false;
        bool pcRight = false;
        bool pcJump = false;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            pcLeft = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
            pcRight = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
            pcJump = keyboard.wKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame;
        }

        // 2. Объединяем управление ПК и Сенсорные Кнопки Android
        bool moveLeft = pcLeft || (mobileLeftButton != null && mobileLeftButton.IsPressed);
        bool moveRight = pcRight || (mobileRightButton != null && mobileRightButton.IsPressed);
        bool jumpPressed = pcJump || (mobileJumpButton != null && mobileJumpButton.IsPressed);

        // 3. Рассчитываем итоговую скорость движения
        horizontalMove = 0f;
        if (moveLeft)
        {
            horizontalMove = -runSpeed;
        }
        else if (moveRight)
        {
            horizontalMove = runSpeed;
        }

        // Анимация бега
        bool isMoving = horizontalMove != 0f;
        if (anim != null)
        {
            anim.SetBool("IsRunning", isMoving);
        }

        // 4. Логика Прыжка
        if (jumpPressed)
        {
            if (!isJumpingInAir)
            {
                jump = true;
                isJumpingInAir = true;

                // Сбрасываем триггер мобильной кнопки, чтобы избежать бесконечного взлёта
                if (mobileJumpButton != null && mobileJumpButton.IsPressed)
                {
                    System.Reflection.PropertyInfo pressedProp = typeof(MobileButton).GetProperty("IsPressed", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (pressedProp != null) pressedProp.SetValue(mobileJumpButton, false);
                }

                if (anim != null)
                {
                    anim.SetBool("IsJumping", true);
                }

                if (AudioManager.Instance != null && jumpSound != null)
                {
                    AudioManager.Instance.PlaySound(jumpSound, 0.7f);
                }
            }
        }

        // 5. Звук шагов
        if (isMoving && !isJumpingInAir && AudioManager.Instance != null)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                AudioManager.Instance.PlaySound(footstepSound, 0.1f);
                footstepTimer = footstepInterval;
            }
        }
        else if (!isMoving || isJumpingInAir)
        {
            footstepTimer = 0f;
        }
    }

    private void FixedUpdate()
    {
        controller.Move(horizontalMove * Time.fixedDeltaTime, jump);
        jump = false;
    }

    public void OnLandEvent()
    {
        isJumpingInAir = false;
        if (anim != null)
        {
            anim.SetBool("IsJumping", false);
        }
    }
}
