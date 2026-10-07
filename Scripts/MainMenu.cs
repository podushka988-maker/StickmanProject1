using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [Header("UI Элементы")]
    [Tooltip("Перетащите сюда ваш объект Canvas из иерархии.")]
    [SerializeField] private GameObject menuCanvas;

    [Header("Игрок")]
    [Tooltip("Перетащите сюда вашего игрока (Player), чтобы включить его при старте игры.")]
    [SerializeField] private GameObject playerObject;

    [Header("Эффекты атмосферы")]
    [Tooltip("Перетащите сюда ваш объект Dust Particles из иерархии.")]
    [SerializeField] private ParticleSystem dustParticles; // Ссылка на систему частиц

    private void Start()
    {
        // При старте игры мы принудительно ВЫКЛЮЧАЕМ игрока, 
        // чтобы он не бегал на заднем фоне меню
        if (playerObject != null)
        {
            playerObject.SetActive(false);
        }
    }

    // Метод для кнопки "Играть"
    public void PlayGame()
    {
        // 1. Полностью выключаем интерфейс меню с экрана
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(false);
        }

        // 2. Включаем игрока — и игра мгновенно начинается прямо здесь!
        if (playerObject != null)
        {
            playerObject.SetActive(true);
        }

        // ЖЕСТКИЙ ФИКС АТМОСФЕРЫ: Насильно будим пылинки и заставляем их летать!
        if (dustParticles != null)
        {
            dustParticles.Play(); // Команда запуска частиц кодом
            Debug.Log("Система частиц Dust Particles успешно запущена кодом из меню!");
        }
    }

    // Метод для кнопки "Выход"
    public void QuitGame()
    {
        Debug.Log("Выход из игры!");
        Application.Quit();
    }
}