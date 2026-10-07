using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class LeverTrigger : MonoBehaviour
{
    [Header("1. НАСТРОЙКА РЫЧАГА")]
    [Tooltip("Объект, который ИЗНАЧАЛЬНО ВКЛЮЧЕН (например, лазер). При повторном щелчке вернется назад.")]
    [SerializeField] private GameObject objectToDisable;

    [Tooltip("Объект, который ИЗНАЧАЛЬНО ВЫКЛЮЧЕН (например, патрульная камера). Можно оставить пустым.")]
    [SerializeField] private GameObject objectToEnable;

    [Header("2. НАСТРОЙКИ UI И ГОРЯЧЕЙ КЛАВИШИ")]
    [Tooltip("Имя тега игрока.")]
    [SerializeField] private string playerTag = "Player";

    [Header("МОБИЛЬНАЯ КНОПКА ДЕЙСТВИЯ")]
    [Tooltip("Перетащите сюда вашу новую кнопку ActionButton из Canvas.")]
    [SerializeField] private MobileButton mobileActionButton;

    [Header("3. ЗВУКОВОЙ ЭФФЕКТ")]
    [Tooltip("Аудиоклип щелчка рычага.")]
    [SerializeField] private AudioClip leverToggleSound;
    [Range(0f, 1f)][SerializeField] private float soundVolume = 0.6f;

    private bool isLeverFlipped = false;
    private bool isPlayerZone = false;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void Update()
    {
        if (isPlayerZone)
        {
            // 1. Проверка для ПК (клавиша E)
            bool pcPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

            // 2. Проверка для телефона (экранная кнопка)
            bool mobilePressed = mobileActionButton != null && mobileActionButton.IsPressed;

            // Если нажата любая из них — переключаем тумблер!
            if (pcPressed || mobilePressed)
            {
                if (mobilePressed)
                {
                    System.Reflection.PropertyInfo pressedProp = typeof(MobileButton).GetProperty("IsPressed", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (pressedProp != null) pressedProp.SetValue(mobileActionButton, false);
                }

                ToggleLever();
            }
        }
    }

    private void ToggleLever()
    {
        isLeverFlipped = !isLeverFlipped;

        if (audioSource != null && leverToggleSound != null)
        {
            audioSource.PlayOneShot(leverToggleSound, soundVolume);
        }

        StopAllCoroutines();
        StartCoroutine(FlickerRoutine(isLeverFlipped));

        string statusMessage = isLeverFlipped ? "Рычаг опущен" : "Рычаг поднят в исходное";
        Debug.Log($"[РЫЧАГ] {gameObject.name}: {statusMessage}.");
    }

    private IEnumerator FlickerRoutine(bool targetState)
    {
        for (int i = 0; i < 3; i++)
        {
            if (objectToDisable != null) objectToDisable.SetActive(false);
            if (objectToEnable != null) objectToEnable.SetActive(true);
            yield return new WaitForSeconds(0.06f);

            if (objectToDisable != null) objectToDisable.SetActive(true);
            if (objectToEnable != null) objectToEnable.SetActive(false);
            yield return new WaitForSeconds(0.06f);
        }

        if (targetState)
        {
            if (objectToDisable != null) objectToDisable.SetActive(false);
            if (objectToEnable != null) objectToEnable.SetActive(true);
        }
        else
        {
            if (objectToDisable != null) objectToDisable.SetActive(true);
            if (objectToEnable != null) objectToEnable.SetActive(false);
        }
    }

    public void ResetLever()
    {
        StopAllCoroutines();
        isLeverFlipped = false;
        isPlayerZone = false;

        if (objectToDisable != null)
        {
            objectToDisable.SetActive(true);
        }

        if (objectToEnable != null)
        {
            objectToEnable.SetActive(false);
        }

        Debug.Log($"[РЫЧАГ] {gameObject.name} намертво сброшен в стартовое положение.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerZone = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerZone = false;
        }
    }
}
