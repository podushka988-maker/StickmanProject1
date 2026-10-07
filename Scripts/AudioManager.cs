using UnityEngine;

/// <summary>
/// Центральный менеджер звуков. Позволяет проигрывать любой звук из любого скрипта.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // Статическая ссылка, чтобы любой скрипт мог обратиться к менеджеру.
    public static AudioManager Instance { get; private set; }

    // "Колонка", которая будет проигрывать звуки.
    private AudioSource audioSource;

    private void Awake()
    {
        // Логика "Одиночки": если менеджер уже есть — удаляем дубликат.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Не удаляем этот объект при смене сцены (чтобы музыка не прерывалась).
        DontDestroyOnLoad(gameObject);

        // Добавляем компонент AudioSource, если его нет.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    /// <summary>
    /// Проигрывает звук один раз. Можно вызывать из любого скрипта.
    /// </summary>
    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
        {
            // PlayOneShot позволяет проигрывать несколько звуков одновременно.
            audioSource.PlayOneShot(clip, volume);
        }
    }
}