using UnityEngine;
public class DiamondPulse : MonoBehaviour
{
    [Header("Настройки пульсации")]
    [Tooltip("Насколько сильно меняется размер.")]
    [SerializeField] private float pulseAmount = 0.15f;

    [Tooltip("Скорость пульсации.")]
    [SerializeField] private float pulseSpeed = 3f;

    private Vector3 baseScale;

    private void Start()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = baseScale * scale;
    }
}