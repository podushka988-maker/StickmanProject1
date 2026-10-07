using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class EndPanelManager : MonoBehaviour
{
    private static EndPanelManager _instance;

    public static EndPanelManager Instance
    {
        get
        {
            if (_instance == null)
            {
                EndPanelManager[] managers = (EndPanelManager[])Resources.FindObjectsOfTypeAll(typeof(EndPanelManager));
                if (managers != null && managers.Length > 0)
                {
                    _instance = managers[0];
                }
                else
                {
                    Debug.LogError("[CRITICAL ERROR] EndLevelPanel object with EndPanelManager script not found on the scene!");
                }
            }
            return _instance;
        }
    }

    [Header("UI ЭЛЕМЕНТЫ ОКНА")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    [Header("ОБЪЕКТЫ ДЛЯ СБРОСА ФИЗИКИ")]
    [SerializeField] private GameObject playerObject;

    [Header("МОБИЛЬНАЯ ПАНЕЛЬ ХОДЬБЫ")]
    [SerializeField] private GameObject mobileControlsPanel;

    [Header("МАССИВ КНОПОК УРОВНЕЙ")]
    [SerializeField] private SimpleLevelLoader[] levelButtons;

    [Header("ДАННЫЕ ИГРЫ")]
    [SerializeField] private int maxTotalLevels = 10;

    private int currentActiveLevel = 1;

    private void Awake()
    {
        if (_instance == null) _instance = this;

        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(ClickNextLevel);
        if (restartButton != null) restartButton.onClick.AddListener(ClickRestart);
        if (menuButton != null) menuButton.onClick.AddListener(ClickMenu);
    }

    private void Start()
    {
        if (PlayerPrefs.GetInt("ShowMenuOnStart", 0) == 1 || (playerObject != null && !playerObject.activeSelf))
        {
            if (mobileControlsPanel != null) mobileControlsPanel.SetActive(false);
        }
    }

    // МЕТОД ПОБЕДЫ: Вызывается из DiamondCollect.cs
    public void ShowVictory(int completedLevel)
    {
        currentActiveLevel = completedLevel;
        SimpleLevelLoader.CurrentActiveLevelNumber = completedLevel; // Синхронизируем глобальный номер миссии
        PrepareAndShowPanel();

        if (statusText != null) statusText.text = "ДЕЛО СДЕЛАНО!\nАЛМАЗ УКРАДЕН";

        int nextLevelNumber = completedLevel + 1;
        int currentSavedProgress = PlayerPrefs.GetInt("MaxUnlockedLevel", 1);

        if (nextLevelNumber > currentSavedProgress && nextLevelNumber <= maxTotalLevels)
        {
            PlayerPrefs.SetInt("MaxUnlockedLevel", nextLevelNumber);
            PlayerPrefs.Save();
        }

        int maxUnlockedLevel = PlayerPrefs.GetInt("MaxUnlockedLevel", 1);

        if (completedLevel >= maxTotalLevels)
        {
            if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
            if (statusText != null) statusText.text = "ИГРА ПРОЙДЕНА!\nВЫ ОГРАБИЛИ МУЗЕЙ!";
            return;
        }

        if (levelButtons != null && currentActiveLevel >= 1 && currentActiveLevel <= levelButtons.Length)
        {
            if (nextLevelButton != null)
            {
                nextLevelButton.gameObject.SetActive(true);

                if (nextLevelNumber <= maxUnlockedLevel)
                {
                    nextLevelButton.interactable = true;
                    Image btnImg = nextLevelButton.GetComponent<Image>();
                    if (btnImg != null) btnImg.color = Color.white;
                }
                else
                {
                    nextLevelButton.interactable = false;
                    Image btnImg = nextLevelButton.GetComponent<Image>();
                    if (btnImg != null) btnImg.color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
                }
            }
        }
    }

    // МЕТОД СМЕРТИ: Вызывается из камер и лазеров
    public void ShowGameOver(int failedLevel)
    {
        currentActiveLevel = failedLevel;
        SimpleLevelLoader.CurrentActiveLevelNumber = failedLevel; // Синхронизируем глобальный номер миссии
        PrepareAndShowPanel();

        if (statusText != null) statusText.text = "ТРЕВОГА!\nШПИОН ПОЙМАН";

        int nextLevelNumber = failedLevel + 1;
        int maxUnlockedLevel = PlayerPrefs.GetInt("MaxUnlockedLevel", 1);

        if (failedLevel >= maxTotalLevels)
        {
            if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
            return;
        }

        if (levelButtons != null && currentActiveLevel >= 1 && currentActiveLevel <= levelButtons.Length)
        {
            if (nextLevelButton != null)
            {
                nextLevelButton.gameObject.SetActive(true);

                if (nextLevelNumber <= maxUnlockedLevel)
                {
                    nextLevelButton.interactable = true;
                    Image btnImg = nextLevelButton.GetComponent<Image>();
                    if (btnImg != null) btnImg.color = Color.white;
                }
                else
                {
                    nextLevelButton.interactable = false;
                    Image btnImg = nextLevelButton.GetComponent<Image>();
                    if (btnImg != null) btnImg.color = new Color(0.3f, 0.3f, 0.3f, 0.4f);
                }
            }
        }
    }

    private void PrepareAndShowPanel()
    {
        gameObject.SetActive(true);
        FreezePlayer(true);

        if (mobileControlsPanel != null) mobileControlsPanel.SetActive(false);

        MuteAllCameras(true);
        Time.timeScale = 0f;

        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    // 🔥 КНОПКА: СЛЕДУЮЩИЙ УРОВЕНЬ (Моментальный запуск строго на спавн СЛЕДУЮЩЕЙ кнопки!)
    private void ClickNextLevel()
    {
        int nextLevelNumber = currentActiveLevel + 1;
        int maxUnlockedLevel = PlayerPrefs.GetInt("MaxUnlockedLevel", 1);

        if (nextLevelNumber > maxUnlockedLevel || currentActiveLevel >= maxTotalLevels)
        {
            return;
        }

        Time.timeScale = 1f;
        MuteAllCameras(false);
        gameObject.SetActive(false);
        FreezePlayer(false);
        ResetAllSceneTraps();

        if (mobileControlsPanel != null && Application.platform == RuntimePlatform.Android)
            mobileControlsPanel.SetActive(true);

        int nextObjectIndex = nextLevelNumber - 1; // Индекс следующей кнопки в массиве
        if (levelButtons != null && nextObjectIndex >= 0 && nextObjectIndex < levelButtons.Length && levelButtons[nextObjectIndex] != null)
        {
            // Обновляем текущий уровень перед прыжком
            currentActiveLevel = nextLevelNumber;
            SimpleLevelLoader.CurrentActiveLevelNumber = nextLevelNumber;

            // Запускаем метод загрузки на следующей кнопке — Стикмен полетит строго на её спавн!
            levelButtons[nextObjectIndex].ClickToLoadLevel();
            Debug.Log($"[МГНОВЕННЫЙ ПЕРЕХОД] Перенос на спавн Уровня {nextLevelNumber} завершен.");
        }
    }

    // 🔥 КНОПКА: ЗАНОВО (Моментальный запуск строго на спавн ТЕКУЩЕЙ кнопки!)
    private void ClickRestart()
    {
        Time.timeScale = 1f;
        MuteAllCameras(false);
        gameObject.SetActive(false);
        FreezePlayer(false);
        ResetAllSceneTraps();

        if (mobileControlsPanel != null && Application.platform == RuntimePlatform.Android)
            mobileControlsPanel.SetActive(true);

        int currentObjectIndex = currentActiveLevel - 1; // Индекс текущей кнопки в массиве
        if (levelButtons != null && currentObjectIndex >= 0 && currentObjectIndex < levelButtons.Length && levelButtons[currentObjectIndex] != null)
        {
            // Запускаем метод загрузки на текущей кнопке — Стикмен полетит строго в начало этой же комнаты!
            levelButtons[currentObjectIndex].ClickToLoadLevel();
            Debug.Log($"[МГНОВЕННЫЙ РЕСТАРТ] Шпион возвращен на спавн Уровня {currentActiveLevel}.");
        }
    }

    private void ClickMenu()
    {
        Time.timeScale = 1f;
        MuteAllCameras(false);
        gameObject.SetActive(false);
        FreezePlayer(false);

        PlayerPrefs.SetInt("ShowMenuOnStart", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void MuteAllCameras(bool mute)
    {
        System.Type camType = typeof(SmartConeDetection);
        Object[] cameras = Resources.FindObjectsOfTypeAll(camType);

        for (int i = 0; i < cameras.Length; i++)
        {
            SmartConeDetection cam = cameras[i] as SmartConeDetection;
            if (cam != null && cam.gameObject.scene.name != null)
            {
                AudioSource source = cam.GetComponent<AudioSource>();
                if (source != null)
                {
                    if (mute) source.Pause();
                    else source.UnPause();
                }
            }
        }
    }

    private void FreezePlayer(bool freeze)
    {
        if (playerObject != null)
        {
            Rigidbody2D rb = playerObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = !freeze;
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private void ResetAllSceneTraps()
    {
        System.Type camType = typeof(SmartConeDetection);
        Object[] cameras = Resources.FindObjectsOfTypeAll(camType);

        for (int i = 0; i < cameras.Length; i++)
        {
            SmartConeDetection cam = cameras[i] as SmartConeDetection;
            if (cam != null && cam.gameObject.scene.name != null)
            {
                cam.gameObject.SetActive(true);
                if (cam.transform.parent != null) cam.transform.parent.gameObject.SetActive(true);
                System.Reflection.FieldInfo suspicionField = typeof(SmartConeDetection).GetField("suspicionTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (suspicionField != null) suspicionField.SetValue(cam, 0f);
                System.Reflection.FieldInfo searchField = typeof(SmartConeDetection).GetField("isSearchingLastPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (searchField != null) searchField.SetValue(cam, false);
            }
        }
        System.Type leverType = typeof(LeverTrigger);
        Object[] levers = Resources.FindObjectsOfTypeAll(leverType);
        for (int i = 0; i < levers.Length; i++)
        {
            LeverTrigger lever = levers[i] as LeverTrigger;
            if (lever != null && lever.gameObject.scene.name != null)
            {
                lever.ResetLever();
            }
        }
    }
}