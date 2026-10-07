using UnityEngine;

public class SmartConeDetection : MonoBehaviour
{
    [Header("1. ТОЧНЫЕ ГРАНИЦЫ ВРАЩЕНИЯ")]
    [SerializeField] private float minAngle = -45f;
    [SerializeField] private float maxAngle = 45f;
    [SerializeField] private float rotationSpeed = 2f;

    [Header("2. РАНДОМНАЯ ПАУЗА НА КРАЯХ")]
    [SerializeField] private float minWaitTime = 0.5f;
    [SerializeField] private float maxWaitTime = 2.5f;

    [Header("3. НАСТРОЙКИ ШПИОНАЖА (ОБНАРУЖЕНИЕ)")]
    [SerializeField] private float timeToAlert = 1.5f;
    [SerializeField] private Color normalColor = new Color(1f, 0.92f, 0.016f, 0.3f);
    [SerializeField] private Color alertColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] private Color cautionColor = new Color(1f, 0.5f, 0f, 0.3f);

    [Header("4. УМНЫЙ ИИ 2.0 (СЛЕЖКА И ПАМЯТЬ)")]
    [SerializeField] private float trackingSpeed = 5f;
    [SerializeField] private float memoryDuration = 3f;
    [SerializeField] private float cautionDuration = 5f;

    [Header("5. ДИСТАНЦИЯ АКТИВАЦИИ ИГРЫ")]
    [SerializeField] private float activationDistance = 30f;

    [Header("6. ЗВУКОВЫЕ ЭФФЕКТЫ")]
    [SerializeField] private AudioClip motorSound;
    [SerializeField] private AudioClip detectionBeepSound;
    [SerializeField] private AudioClip escapeSound;

    [Space]
    [Range(0f, 1f)][SerializeField] private float motorVolume = 0.003f;
    [Range(0f, 1f)][SerializeField] private float beepVolume = 0.2f;
    [Range(0f, 1f)][SerializeField] private float escapeVolume = 0.5f;

    private float targetZRotation;
    private bool movingToMax = true;
    private float waitTimer = 0f;
    private bool isPlayerInside = false;
    private float suspicionTimer = 0f;
    private MeshRenderer coneMeshRenderer;
    private Transform playerTransform;
    private Vector3 lastKnownPlayerPosition;
    private bool isSearchingLastPosition = false;
    private float memoryTimer = 0f;
    private bool isCautionActive = false;
    private float cautionTimer = 0f;
    private float beepTimer = 0f;
    private AudioSource motorAudioSource;

    private GameObject mainMenuPanel;
    private GameObject levelSelectionPanel;
    private Transform rotationTarget;
    private float autoAngleOffset = 0f;

    public bool IsPlayerSpotted => isPlayerInside;

    private void Awake()
    {
        coneMeshRenderer = GetComponent<MeshRenderer>();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        if (motorSound != null)
        {
            motorAudioSource = gameObject.AddComponent<AudioSource>();
            motorAudioSource.clip = motorSound;
            motorAudioSource.loop = true;
            motorAudioSource.volume = motorVolume;
            motorAudioSource.playOnAwake = false;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null) playerTransform = player.transform;

        rotationTarget = transform.parent != null ? transform.parent : transform;

        if (transform.parent != null)
        {
            autoAngleOffset = transform.localEulerAngles.z;
        }
    }

    private void Start()
    {
        targetZRotation = maxAngle;
        mainMenuPanel = GameObject.Find("MainMenuPanel");
        levelSelectionPanel = GameObject.Find("LevelSelectionPanel");
    }

    private void Update()
    {
        bool isAnyMenuOpen = (mainMenuPanel != null && mainMenuPanel.activeSelf) ||
                             (levelSelectionPanel != null && levelSelectionPanel.activeSelf);

        if (isAnyMenuOpen)
        {
            if (motorAudioSource != null && motorAudioSource.isPlaying) motorAudioSource.Stop();
            return;
        }

        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer > activationDistance)
            {
                if (motorAudioSource != null && motorAudioSource.isPlaying) motorAudioSource.Stop();
                return;
            }
        }
        else
        {
            return;
        }

        if (motorAudioSource != null) motorAudioSource.volume = motorVolume;

        if (isPlayerInside && playerTransform != null)
        {
            isSearchingLastPosition = false;
            isCautionActive = false;

            suspicionTimer += Time.deltaTime;
            UpdateColorMode();
            PlayDetectionBeep();

            lastKnownPlayerPosition = playerTransform.position;

            if (motorAudioSource != null && !motorAudioSource.isPlaying) motorAudioSource.Play();

            float currentTrackingSpeed = trackingSpeed * (1f + (suspicionTimer / timeToAlert));
            TrackTargetPosition(lastKnownPlayerPosition, currentTrackingSpeed);

            // ТРЕВОГА ПОЛНОСТЬЮ НАКОПИЛАСЬ 🚨
            if (suspicionTimer >= timeToAlert)
            {
                Debug.LogWarning("[ИИ КАМЕРЫ] Полоса обнаружения полная! Включаем окно поражения.");

                suspicionTimer = 0f;
                isPlayerInside = false;

                // 🔥 ВОЗВРАЩАЕМ ВЫЗОВ ОКНА СМЕРТИ:
                if (EndPanelManager.Instance != null)
                {
                    EndPanelManager.Instance.ShowGameOver(SimpleLevelLoader.CurrentActiveLevelNumber);
                }

                return;
            }
        }
        else
        {
            if (suspicionTimer > 0f)
            {
                suspicionTimer -= Time.deltaTime * 1.2f;
                UpdateColorMode();
            }
            else
            {
                suspicionTimer = 0f;
                if (!isSearchingLastPosition && !isCautionActive) UpdateColor(0f, normalColor, normalColor);
            }
        }

        if (isPlayerInside) return;

        if (isSearchingLastPosition)
        {
            memoryTimer -= Time.deltaTime;
            TrackTargetPosition(lastKnownPlayerPosition, trackingSpeed);

            float lookAround = Mathf.Sin(Time.time * 4f) * 5f;
            rotationTarget.Rotate(0f, 0f, lookAround * Time.deltaTime);

            if (motorAudioSource != null && motorAudioSource.isPlaying) motorAudioSource.Stop();

            if (memoryTimer <= 0f)
            {
                isSearchingLastPosition = false;
                isCautionActive = true;
                cautionTimer = cautionDuration;
            }
            return;
        }

        if (isCautionActive)
        {
            cautionTimer -= Time.deltaTime;
            UpdateColorMode();
            if (cautionTimer <= 0f) isCautionActive = false;
        }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            if (motorAudioSource != null && motorAudioSource.isPlaying) motorAudioSource.Stop();
            return;
        }

        if (motorAudioSource != null && !motorAudioSource.isPlaying) motorAudioSource.Play();

        float currentZ = rotationTarget.eulerAngles.z;
        if (currentZ > 180f) currentZ -= 360f;

        float targetCorrection = targetZRotation;
        if (targetCorrection > 180f) targetCorrection -= 360f;

        float currentSpeed = isCautionActive ? (rotationSpeed * 1.5f) : rotationSpeed;
        float newZ = Mathf.MoveTowardsAngle(currentZ, targetCorrection, currentSpeed * Time.deltaTime * 10f);

        rotationTarget.rotation = Quaternion.Euler(0f, 0f, newZ);

        if (Mathf.Abs(Mathf.DeltaAngle(newZ, targetCorrection)) < 0.1f)
        {
            float currentWaitTime = isCautionActive ? (Random.Range(minWaitTime, maxWaitTime) * 0.5f) : Random.Range(minWaitTime, maxWaitTime);
            waitTimer = currentWaitTime;
            movingToMax = !movingToMax;
            targetZRotation = movingToMax ? maxAngle : minAngle;
        }
    }

    private void TrackTargetPosition(Vector3 targetPos, float speed)
    {
        Vector3 worldDirection = targetPos - rotationTarget.position;
        worldDirection.z = 0f;

        if (worldDirection.sqrMagnitude > 0.01f)
        {
            float finalZ = 0f;
            if (rotationTarget.parent != null)
            {
                Vector3 localDirection = rotationTarget.parent.InverseTransformDirection(worldDirection);
                float localAngle = Mathf.Atan2(localDirection.y, localDirection.x) * Mathf.Rad2Deg;
                finalZ = localAngle;
            }
            else
            {
                float worldAngle = Mathf.Atan2(worldDirection.y, worldDirection.x) * Mathf.Rad2Deg;
                finalZ = worldAngle - 90f;
            }

            if (finalZ > 180f) finalZ -= 360f;
            if (finalZ < -180f) finalZ += 360f;

            rotationTarget.localRotation = Quaternion.Lerp(rotationTarget.localRotation, Quaternion.Euler(0f, 0f, finalZ - autoAngleOffset), Time.deltaTime * speed);
        }
    }

    private void UpdateColorMode()
    {
        if (isPlayerInside || suspicionTimer > 0f) UpdateColor(suspicionTimer / timeToAlert, normalColor, alertColor);
        else if (isCautionActive) UpdateColor(cautionTimer / cautionDuration, normalColor, cautionColor);
    }

    private void UpdateColor(float progress, Color startColor, Color endColor)
    {
        if (coneMeshRenderer == null || coneMeshRenderer.material == null) return;
        progress = Mathf.Clamp01(progress);
        Color currentColor = Color.Lerp(startColor, endColor, progress);
        if (coneMeshRenderer.material.HasProperty("_BaseColor")) coneMeshRenderer.material.SetColor("_BaseColor", currentColor);
        else if (coneMeshRenderer.material.HasProperty("_Color")) coneMeshRenderer.material.color = currentColor;
    }

    private void PlayDetectionBeep()
    {
        if (detectionBeepSound == null || AudioManager.Instance == null) return;
        beepTimer -= Time.deltaTime;
        if (beepTimer <= 0f)
        {
            float progress = suspicionTimer / timeToAlert;
            AudioManager.Instance.PlaySound(detectionBeepSound, beepVolume);
            beepTimer = Mathf.Lerp(0.4f, 0.1f, progress);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerInside = true;
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = false;
            beepTimer = 0f;
            if (suspicionTimer > (timeToAlert * 0.12f))
            {
                if (AudioManager.Instance != null && escapeSound != null) AudioManager.Instance.PlaySound(escapeSound, escapeVolume);
                isSearchingLastPosition = true;
                memoryTimer = memoryDuration;
            }
        }
    }
}