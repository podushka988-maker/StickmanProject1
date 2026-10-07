using UnityEngine;

public class FogOfWarHider : MonoBehaviour
{
    [Header("Настройки скрытия")]
    [Tooltip("Если объект — это враг или камера, включите это, чтобы они ИСЧЕЗАЛИ, когда вы уходите. Для алмазов — выключите.")]
    [SerializeField] private bool hideCompletelyInExplored = false;

    [Tooltip("Прозрачность алмаза, когда игрок от него отошел (от 0 до 1).")]
    [Range(0f, 1f)][SerializeField] private float exploredAlpha = 0.4f;

    private SpriteRenderer spriteRenderer;
    private float updateTimer;
    private const float UPDATE_INTERVAL = 0.1f;

    // ГЛАВНОЕ ИСПРАВЛЕНИЕ: Флаг памяти объекта
    private bool hasBeenRevealed = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // Изначально прячем объект, пока игрок его не найдет
        if (spriteRenderer != null) spriteRenderer.enabled = false;
    }

    private void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer < UPDATE_INTERVAL) return;
        updateTimer = 0f;

        if (FogOfWarManager.InstanceTexture == null) return;

        // Автоматически находим параметры камеры из менеджера тумана
        FogOfWarManager manager = FindAnyObjectByType<FogOfWarManager>();
        Transform camTex = Camera.main.transform;
        Vector2 viewArea = new Vector2(30f, 20f); // Должно строго совпадать с вашим менеджером тумана

        if (manager != null && manager.cameraTransform != null)
        {
            camTex = manager.cameraTransform;
            viewArea = manager.currentViewArea;
        }

        // Считаем позицию объекта на экране
        Vector3 relativePos = transform.position - camTex.position;
        Vector2 posNormalized = new Vector2(
            (relativePos.x + viewArea.x / 2f) / viewArea.x,
            (relativePos.y + viewArea.y / 2f) / viewArea.y
        );

        // Если объект за экраном и еще не был найден — скрываем его
        if (posNormalized.x < 0 || posNormalized.x > 1 || posNormalized.y < 0 || posNormalized.y > 1)
        {
            if (!hasBeenRevealed && spriteRenderer != null) spriteRenderer.enabled = false;
            return;
        }

        int texSize = FogOfWarManager.InstanceTexture.width;
        int pixelX = Mathf.Clamp(Mathf.RoundToInt(posNormalized.x * texSize), 0, texSize - 1);
        int pixelY = Mathf.Clamp(Mathf.RoundToInt(posNormalized.y * texSize), 0, texSize - 1);

        Color32 pixelColor = FogOfWarManager.InstanceTexture.GetPixel(pixelX, pixelY);
        byte alpha = pixelColor.a;

        if (spriteRenderer != null)
        {
            // 1. Прямо сейчас находится в круге яркого света (Альфа тумана близка к 0)
            if (alpha < 50)
            {
                hasBeenRevealed = true; // ЗАПОМИНАЕМ: свет коснулся этого объекта!
                spriteRenderer.enabled = true;
                SetSpriteAlpha(1f); // Полная яркость
            }
            // 2. Объект в тусклой (уже исследованной) зоне
            else if (alpha >= 50 && alpha < 250)
            {
                if (hasBeenRevealed)
                {
                    // Если алмаз уже находили, он НЕ исчезнет. Он просто станет тусклым!
                    if (hideCompletelyInExplored)
                    {
                        spriteRenderer.enabled = false; // Для врагов
                    }
                    else
                    {
                        spriteRenderer.enabled = true; // Для алмазов
                        SetSpriteAlpha(exploredAlpha); // Делаем полупрозрачным
                    }
                }
                else
                {
                    // Если зона тусклая (например, от Global Light 0.2), но конусом мы сюда еще не светили
                    spriteRenderer.enabled = false;
                }
            }
            // 3. Полная, неисследованная темнота (Альфа тумана = 255)
            else
            {
                // Если алмаз уже был найден ранее, он остается виден в темноте тусклым силуэтом (карта памяти)
                if (hasBeenRevealed && !hideCompletelyInExplored)
                {
                    spriteRenderer.enabled = true;
                    SetSpriteAlpha(exploredAlpha);
                }
                else
                {
                    // Если никогда не видели — полная невидимость
                    spriteRenderer.enabled = false;
                }
            }
        }
    }

    // Вспомогательный метод для плавного изменения прозрачности самого спрайта
    private void SetSpriteAlpha(float alphaValue)
    {
        if (spriteRenderer == null) return;
        Color c = spriteRenderer.color;
        c.a = alphaValue;
        spriteRenderer.color = c;
    }
}