using UnityEngine;
using System.Collections;

public class LaserDamage : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Тег объекта, который считается игроком.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Слои, которые блокируют обзор (стены).")]
    [SerializeField] private LayerMask obstacleLayer;

    [Tooltip("Точка, из которой исходит луч (глаз камеры).")]
    [SerializeField] private Transform raycastOrigin;

    [Header("Звуки")]
    [Tooltip("Звук смерти при попадании.")]
    [SerializeField] private AudioClip deathSound;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (IsViewBlocked(other.transform)) return;

        TriggerGameOverProcess();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (IsViewBlocked(other.transform)) return;

        TriggerGameOverProcess();
    }

    private void TriggerGameOverProcess()
    {
        if (AudioManager.Instance != null && deathSound != null)
        {
            AudioManager.Instance.PlaySound(deathSound);
        }

        Debug.LogWarning($"[БЕЗОПАСНОСТЬ] Стикмен задел смертоносный лазер '{gameObject.name}'!");

        // 🔥 ВОЗВРАЩАЕМ ВЫЗОВ ОКНА СМЕРТИ:
        if (EndPanelManager.Instance != null)
        {
            EndPanelManager.Instance.ShowGameOver(SimpleLevelLoader.CurrentActiveLevelNumber);
        }
    }

    private bool IsViewBlocked(Transform target)
    {
        Vector2 origin = raycastOrigin != null
            ? (Vector2)raycastOrigin.position
            : (Vector2)transform.position;

        Vector2 targetPosition = (Vector2)target.position;

        RaycastHit2D hit = Physics2D.Linecast(
            origin,
            targetPosition,
            obstacleLayer
        );

        return hit.collider != null;
    }
}
