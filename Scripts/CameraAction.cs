using UnityEngine;
public class CameraAction : MonoBehaviour
{
    [Header("Настройки вращения")]
    [Tooltip("Скорость вращения в градусах в секунду.")]
    [SerializeField] private float rotationSpeed = 40f;

    [Tooltip("Крайний левый угол поворота. Отрицательное значение.")]
    [SerializeField] private float minAngle = -60f;

    [Tooltip("Крайний правый угол поворота. Положительное значение.")]
    [SerializeField] private float maxAngle = 60f;
    private float currentAngle;
    private int rotationDirection = 1;

    private void Start()
    {
        currentAngle = minAngle;
        rotationDirection = 1;

        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    private void Update()
    {
        currentAngle += rotationSpeed * rotationDirection * Time.deltaTime;
        if (currentAngle >= maxAngle)
        {
            currentAngle = maxAngle;
            rotationDirection = -1;
        }
        else if (currentAngle <= minAngle)
        {
            currentAngle = minAngle;
            rotationDirection = 1;
        }
        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }
}