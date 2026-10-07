using UnityEngine;
using System.Collections;

public class DiamondCollect : MonoBehaviour
{
    [Header("НАСТРОЙКИ МИССИИ")]
    [Tooltip("Какой честный номер у ЭТОГО алмаза (1, 2, 3...)")]
    [SerializeField] private int currentLevelNumber = 1;

    [Tooltip("Тег объекта, который считается игроком.")]
    [SerializeField] private string playerTag = "Player";

    [Header("ЗВУКОВОЙ ЭФФЕКТ")]
    [Tooltip("Звук успешного сбора алмаза.")]
    [SerializeField] private AudioClip collectSound;

    private bool isCollected = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected || !other.CompareTag(playerTag)) return;

        // 🔥 ШПИОНСКАЯ ПРОВЕРКА: Проверяем, не засёк ли нас кто-то прямо сейчас
        if (IsPlayerSpottedByAnyCamera())
        {
            Debug.LogWarning($"[СИСТЕМА БЕЗОПАСНОСТИ] Алмаз заблокирован! Камеры видят шпиона, кража невозможна!");
            return; // Мгновенно отменяем сбор алмаза! Стикмен просто пройдёт сквозь него.
        }

        // Если всё чисто — запускаем процесс победы
        StartCoroutine(DelayedVictoryRoutine());
    }

    private IEnumerator DelayedVictoryRoutine()
    {
        isCollected = true;

        if (AudioManager.Instance != null && collectSound != null)
        {
            AudioManager.Instance.PlaySound(collectSound);
        }

        // Тушим мобильную панель ходьбы перед задержкой, чтобы очистить экра


        // Ждем 0.5 секунд сочной геймплейной паузы
        yield return new WaitForSecondsRealtime(0);

        if (EndPanelManager.Instance != null)
        {
            EndPanelManager.Instance.ShowVictory(currentLevelNumber);
        }

        gameObject.SetActive(false);
        isCollected = false;
    }

    // Вспомогательный метод: проверяет статус у всех живых камер на текущем уровне
    private bool IsPlayerSpottedByAnyCamera()
    {
        // Бронебойный классический поиск активных камер на сцене
        SmartConeDetection[] allCameras = Object.FindObjectsOfType<SmartConeDetection>();

        foreach (var camera in allCameras)
        {
            if (camera != null)
            {
                // Если у этой камеры горит флаг, что игрок сейчас внутри её конуса света:
                if (camera.IsPlayerSpotted)
                {
                    return true; // Шпион обнаружен! Кража заблокирована.
                }
            }
        }
        return false; // Всё чисто, патрули смотрят в другую сторону!
    }
}
