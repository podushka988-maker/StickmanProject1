using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class VisionCone : MonoBehaviour
{
    [Header("Размеры луча")]
    [Tooltip("Максимальная длина луча в юнитах.")]
    [SerializeField] private float length = 6f;

    [Tooltip("Угол раствора конуса в градусах.")]
    [SerializeField] private float fieldOfView = 60f;

    [Tooltip("Сколько лучей пускать. Больше — глаже края, но дороже.")]
    [SerializeField] private int rayCount = 40;

    [Header("Внешний вид (Желтый)")]
    [SerializeField] private Color beamColor = new Color(1f, 0.92f, 0.016f, 0.4f); // Яркий полупрозрачный желтый

    [Header("Препятствия")]
    [Tooltip("ВАЖНО: Выберите слой стен (например, Ground). Убедитесь, что у стен стоит Box Collider 2D!")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Слои отрисовки меша")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 0;

    private Mesh visionMesh;
    private MeshRenderer meshRenderer;
    private PolygonCollider2D polyCollider;

    private void Awake()
    {
        // Отключаем столкновение лучей с собственным коллайдером конуса/игрока
        Physics2D.queriesStartInColliders = false;

        visionMesh = new Mesh();
        visionMesh.name = "VisionConeMesh";
        GetComponent<MeshFilter>().mesh = visionMesh;

        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;

        polyCollider = GetComponent<PolygonCollider2D>();
        polyCollider.isTrigger = true;

        SetupYellowMaterial();
    }

    private void SetupYellowMaterial()
    {
        // Используем стандартный освещаемый 2D шейдер, чтобы реагировать на Point Light 2D
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        material.color = beamColor;
        meshRenderer.material = material;
    }

    private void LateUpdate()
    {
        DrawVisionConeAndCollider();
    }

    private void DrawVisionConeAndCollider()
    {
        int vertexCount = rayCount + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];
        Vector2[] colliderPoints = new Vector2[vertexCount];
        int[] triangles = new int[rayCount * 3];

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);
        colliderPoints[0] = Vector2.zero;

        float startAngle = -fieldOfView / 2f;
        float angleStep = fieldOfView / rayCount;

        for (int i = 0; i <= rayCount; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 worldDirection = DirectionFromAngle(angle);

            // Пускаем луч. Он игнорирует триггеры и ищет только слой стен
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                worldDirection,
                length,
                obstacleLayer
            );

            Vector3 worldPoint;
            if (hit.collider != null)
            {
                worldPoint = hit.point;
            }
            else
            {
                worldPoint = transform.position + worldDirection * length;
            }

            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            vertices[i + 1] = localPoint;
            colliderPoints[i + 1] = new Vector2(localPoint.x, localPoint.y);

            float rad = angle * Mathf.Deg2Rad;
            float distNormalized = localPoint.magnitude / length;
            uv[i + 1] = new Vector2(
                0.5f + Mathf.Cos(rad) * 0.5f * distNormalized,
                0.5f + Mathf.Sin(rad) * 0.5f * distNormalized
            );
        }

        for (int i = 0; i < rayCount; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        visionMesh.Clear();
        visionMesh.vertices = vertices;
        visionMesh.triangles = triangles;
        visionMesh.uv = uv;
        visionMesh.RecalculateBounds();
        visionMesh.RecalculateNormals();

        polyCollider.pathCount = 1;
        polyCollider.SetPath(0, colliderPoints);
    }

    private Vector3 DirectionFromAngle(float angleInDegrees)
    {
        float totalAngle = angleInDegrees + transform.eulerAngles.z;
        float radians = totalAngle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
    }
}