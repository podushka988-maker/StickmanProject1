using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SimpleLevelLoader : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Глобальная переменная для камер
    public static int CurrentActiveLevelNumber { get; set; } = 1;

    [Header("0. НАСТРОЙКА ПРОГРЕССА КНОПКИ")]
    [Tooltip("Какой номер уровня запускает ЭТА конкретная кнопка (1, 2, 3...)")]
    [SerializeField] private int currentButtonLevelNumber = 1;

    [Tooltip("Сколько ВСЕГО уровней создано в вашей игре прямо сейчас.")]
    [SerializeField] private int maxTotalLevels = 10;

    [Header("1. ТЕКСТУРА КУРСОРA-РУКИ")]
    [SerializeField] private Texture2D handCursorTexture;

    [Header("2. ССЫЛКИ НА ПЕРСОНАЖА И КАМЕРУ")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject cameraObject;

    [Header("3. АЛМАЗ ЭТОГО УРОВНЯ (ДЛЯ РЕСПАВНА)")]
    [SerializeField] private GameObject levelDiamond;

    [Header("4. ССЫЛКИ НА ПАНЕЛИ UI")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject levelSelectionPanel;

    [Header("5. ТОЧКА СПАВНА")]
    [SerializeField] private Transform targetSpawnPoint;

    private Button myButtonComponent;

   private void Start()
    {
        int maxUnlockedLevel = PlayerPrefs.GetInt("MaxUnlockedLevel", 1);
        if (maxUnlockedLevel > maxTotalLevels) maxUnlockedLevel = maxTotalLevels;

        myButtonComponent = GetComponent<Button>();
        Image myImageComponent = GetComponent<Image>();

        bool isImageValid = !System.Object.ReferenceEquals(myImageComponent, null) && myImageComponent;

        if (myButtonComponent != null)
        {
            if (currentButtonLevelNumber <= maxUnlockedLevel)
            {
                myButtonComponent.interactable = true;
                if (isImageValid) myImageComponent.color = Color.white;
            }
            else
            {
                myButtonComponent.interactable = false;
                if (isImageValid) myImageComponent.color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
            }
        }

        if (isImageValid)
        {
            myImageComponent.alphaHitTestMinimumThreshold = 0.1f;
        }

        // 🔥 ЖЕЛЕЗОБЕТОННЫЙ МОБИЛЬНЫЙ ФИКС ДЛЯ ПЕРВОГО ЗАПУСКА:
        // Проверяем: если мы на телефоне и ВСЕ панели меню выключены — принудительно зажигаем Главное Меню!
        if (mainMenuPanel != null && levelSelectionPanel != null)
        {
            // Если игра просит показать меню выбора уровней
            if (PlayerPrefs.GetInt("ShowMenuOnStart", 0) == 1)
            {
                PlayerPrefs.SetInt("ShowMenuOnStart", 0);
                PlayerPrefs.Save();

                levelSelectionPanel.SetActive(true);
                mainMenuPanel.SetActive(false);
                if (playerObject != null) playerObject.SetActive(false);
            }
            // ЕДИНСТВЕННЫЙ И ПРАВИЛЬНЫЙ ВЫХОД ДЛЯ ЧИСТОГО ТЕЛЕФОНА:
            else if (!mainMenuPanel.activeSelf && !levelSelectionPanel.activeSelf && PlayerPrefs.GetInt("AutoReloadLevel", 0) == 0)
            {
                mainMenuPanel.SetActive(true); // Зажигаем главное меню перед глазами!
                if (playerObject != null) playerObject.SetActive(false); // Прячем стикмена, пока он в меню
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (myButtonComponent != null && myButtonComponent.interactable)
        {
            if (handCursorTexture != null) Cursor.SetCursor(handCursorTexture, Vector2.zero, CursorMode.Auto);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private void OnDisable()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    // Публичный метод, который можно безопасно вызвать снаружи
    public void ClickToLoadLevel()
    {
        // 1. МГНОВЕННО ГАСИМ ОКНА МЕНЮ (Строго первой строчкой, чтобы они гарантированно исчезали на ПК!)
        if (levelSelectionPanel != null) levelSelectionPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

        // 2. Очищаем старые зависшие флаги памяти
        PlayerPrefs.SetInt("ShowMenuOnStart", 0);
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        CurrentActiveLevelNumber = currentButtonLevelNumber;
        Debug.Log($"[СИСТЕМА] Запущен Уровень {CurrentActiveLevelNumber}. Панели меню скрыты.");

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (levelDiamond != null) levelDiamond.SetActive(true);

        // 3. Рассчитываем точку спавна
        Vector3 spawnPosition = Vector3.zero;
        if (targetSpawnPoint != null)
        {
            spawnPosition = targetSpawnPoint.position;
        }
        else
        {
            string fallbackSpawnName = "Spawn" + currentButtonLevelNumber;
            GameObject fallbackSpawn = GameObject.Find(fallbackSpawnName);
            if (fallbackSpawn != null) spawnPosition = fallbackSpawn.transform.position;
            else spawnPosition = currentButtonLevelNumber == 2 ? new Vector3(104f, 5f, 0f) : new Vector3(0f, 2f, 0f);
        }

        // 4. Переносим и активируем шпиона Стикмена
        if (playerObject != null)
        {
            playerObject.SetActive(true);
            Rigidbody2D rb = playerObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = false;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            playerObject.transform.position = spawnPosition;
            if (rb != null) rb.simulated = true;
        }

        // 5. Центрируем шпионскую камеру слежения
        if (cameraObject != null)
        {
            cameraObject.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, -10f);
        }

        // 6. ВКЛЮЧАЕМ СЕНСОРНЫЙ ИНТЕРФЕЙС ТОЛЬКО ЕСЛИ ИГРАЕМ НА ТЕЛЕФОНЕ ANDROID
        if (Application.platform == RuntimePlatform.Android)
        {
            GameObject mobPanel = GameObject.Find("Canvas")?.transform.Find("MobileControlsPanel")?.gameObject;
            if (mobPanel != null) mobPanel.SetActive(true);
        }
    }
}