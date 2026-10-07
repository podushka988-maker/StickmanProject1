using UnityEngine;

public class FogOfWarManager : MonoBehaviour
{
    [Header("Настройки текстуры")]
    [Tooltip("Разрешение текстуры тумана. 256 или 512 вполне достаточно.")]
    [SerializeField] private int textureSize = 256;

    [Header("Игрок и свет")]
    [Tooltip("Ссылка на трансформ игрока.")]
    [SerializeField] public Transform playerTransform;
    [Tooltip("Радиус раскрытия тумана вокруг игрока.")]
    [SerializeField] private float revealRadius = 5f;
    [Tooltip("Насколько сильно затухают края раскрытой зоны.")]
    [Range(0.1f, 1f)][SerializeField] private float blurScale = 0.5f;

    [Header("Шпионские преграды")]
    [Tooltip("Слой стен (выберите Ground), который будет физически обрубать туман войны.")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Настройки алмазов и ориентиров")]
    [Tooltip("Перетащите сюда ваши алмазы со сцены, чтобы они светились сквозь темноту.")]
    [SerializeField] private Transform[] diamondTransforms;
    [Tooltip("Радиус раскрытия тумана вокруг алмаза (ориентира).")]
    [SerializeField] private float diamondRevealRadius = 3f;

    [Header("Настройки динамических границ")]
    [Tooltip("Ссылка на главную камеру.")]
    [SerializeField] public Transform cameraTransform;
    [Tooltip("Размер области мира, которую текстура covers одновременно (например, 30х30).")]
    [SerializeField] public Vector2 currentViewArea = new Vector2(30f, 30f);

    public Texture2D fogTexture;
    public Color32[] textureColors;

    public static Texture2D InstanceTexture { get; private set; }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        InitializeFogTexture();
    }

    private void InitializeFogTexture()
    {
        fogTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        fogTexture.wrapMode = TextureWrapMode.Clamp;

        textureColors = new Color32[textureSize * textureSize];

        for (int i = 0; i < textureColors.Length; i++)
        {
            textureColors[i] = new Color32(0, 0, 0, 255); // Изначально полная темнота
        }

        fogTexture.SetPixels32(textureColors);
        fogTexture.Apply();

        InstanceTexture = fogTexture;
        Shader.SetGlobalTexture("_FogTex", fogTexture);
    }

    private void Update()
    {
        // 1. Постепенное возвращение тумана (рассеивание старого света)
        for (int i = 0; i < textureColors.Length; i++)
        {
            if (textureColors[i].a < 70)
            {
                textureColors[i].a = (byte)Mathf.MoveTowards(textureColors[i].a, 150, Time.deltaTime * 25f);
            }
        }

        // 2. ПРОЖИГАНИЕ ТУМАНА ВОКРУГ ИГРОКА С УЧЕТОМ ФИЗИКИ СТЕН (Умные лучи)
        if (playerTransform != null)
        {
            RevealPositionWithPhysics(playerTransform.position, revealRadius);
        }

        // 3. ПРОЖИГАНИЕ ТУМАНА ВОКРУГ ВСЕХ АЛМАЗОВ (Ориентиры горят без лучей, сквозь стены)
        if (diamondTransforms != null)
        {
            foreach (Transform diamond in diamondTransforms)
            {
                if (diamond != null)
                {
                    RevealPositionSimple(diamond.position, diamondRevealRadius);
                }
            }
        }

        // Применяем изменения к текстуре в конце кадра один раз
        fogTexture.SetPixels32(textureColors);
        fogTexture.Apply();
    }

    // РЕАЛИСТИЧНЫЙ МЕТОД: Проверяет физические препятствия лучами Raycast
    private void RevealPositionWithPhysics(Vector3 originWorldPos, float radius)
    {
        Vector3 relativeOrigin = originWorldPos - cameraTransform.position;
        Vector2 originNormalized = new Vector2(
            (relativeOrigin.x + currentViewArea.x / 2f) / currentViewArea.x,
            (relativeOrigin.y + currentViewArea.y / 2f) / currentViewArea.y
        );

        int centerX = Mathf.RoundToInt(originNormalized.x * textureSize);
        int centerY = Mathf.RoundToInt(originNormalized.y * textureSize);
        int radiusInPixels = Mathf.RoundToInt((radius / currentViewArea.x) * textureSize);

        for (int x = centerX - radiusInPixels; x <= centerX + radiusInPixels; x++)
        {
            for (int y = centerY - radiusInPixels; y <= centerY + radiusInPixels; y++)
            {
                if (x >= 0 && x < textureSize && y >= 0 && y < textureSize)
                {
                    float dist = Vector2.Distance(new Vector2(centerX, centerY), new Vector2(x, y));
                    if (dist < radiusInPixels)
                    {
                        // 1. Переводим пиксель тумана в реальный МИР Unity
                        Vector2 pixelWorldPos = new Vector2(
                            ((float)x / textureSize) * currentViewArea.x - (currentViewArea.x / 2f) + cameraTransform.position.x,
                            ((float)y / textureSize) * currentViewArea.y - (currentViewArea.y / 2f) + cameraTransform.position.y
                        );

                        // 2. Считаем направление луча от стикмена к этой точке
                        Vector2 direction = pixelWorldPos - (Vector2)originWorldPos;
                        float worldDist = direction.magnitude;

                        // 3. ЭФФЕКТНЫЙ ФИКС КОНТУРОВ: Пускаем не тонкую ниточку, а ЛУЧ-СФЕРУ радиусом 0.1
                        // Она физически не способна проскочить сквозь Outlines-линии и жестко обрубает свет по краям!
                        RaycastHit2D hit = Physics2D.CircleCast(originWorldPos, 0.1f, direction.normalized, worldDist, obstacleLayer);

                        // Если сфера зацепила контур стены — блокируем раскрытие тумана наружу
                        if (hit.collider != null)
                        {
                            continue;
                        }

                        // Если путь чист — плавно выжигаем туман
                        int index = y * textureSize + x;
                        float factor = dist / radiusInPixels;
                        byte targetAlpha = (byte)Mathf.Lerp(0, 150, Mathf.Pow(factor, 1f / blurScale));

                        if (targetAlpha < textureColors[index].a)
                        {
                            textureColors[index].a = targetAlpha;
                        }
                    }
                }
            }
        }
    }

    // Стандартный плоский метод для алмазов
    private void RevealPositionSimple(Vector3 targetWorldPos, float radius)
    {
        Vector3 relativePos = targetWorldPos - cameraTransform.position;
        Vector2 posNormalized = new Vector2(
            (relativePos.x + currentViewArea.x / 2f) / currentViewArea.x,
            (relativePos.y + currentViewArea.y / 2f) / currentViewArea.y
        );

        int centerX = Mathf.RoundToInt(posNormalized.x * textureSize);
        int centerY = Mathf.RoundToInt(posNormalized.y * textureSize);
        int radiusInPixels = Mathf.RoundToInt((radius / currentViewArea.x) * textureSize);

        for (int x = centerX - radiusInPixels; x <= centerX + radiusInPixels; x++)
        {
            for (int y = centerY - radiusInPixels; y <= centerY + radiusInPixels; y++)
            {
                if (x >= 0 && x < textureSize && y >= 0 && y < textureSize)
                {
                    float dist = Vector2.Distance(new Vector2(centerX, centerY), new Vector2(x, y));
                    if (dist < radiusInPixels)
                    {
                        int index = y * textureSize + x;
                        float factor = dist / radiusInPixels;
                        byte targetAlpha = (byte)Mathf.Lerp(0, 150, Mathf.Pow(factor, 1f / blurScale));

                        if (targetAlpha < textureColors[index].a)
                        {
                            textureColors[index].a = targetAlpha;
                        }
                    }
                }
            }
        }
    }
}