using UnityEngine;
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PlayerLight : MonoBehaviour
{
    [Header("Размер света")]
    [Tooltip("Радиус света в юнитах.")]
    [SerializeField] private float radius = 10f;

    [Tooltip("Сколько лучей пускать. Больше — глаже край, но дороже.")]
    [SerializeField] private int rayCount = 64;

    [Header("Препятствия")]
    [Tooltip("Слои, которые блокируют свет (стены).")]
    [SerializeField] private LayerMask obstacleLayer;

    private Mesh lightMesh;

    private void Awake()
    {
        lightMesh = new Mesh();
        lightMesh.name = "PlayerLightMesh";
        GetComponent<MeshFilter>().mesh = lightMesh;

        BuildMaterial();
    }

    private void BuildMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Transparent");
        }

        Material material = new Material(shader);
        material.color = new Color(1f, 0.95f, 0.8f, 0.55f);

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.material = material;
    }

    private void LateUpdate()
    {
        BuildLightMesh();
    }

    private void BuildLightMesh()
    {
        int vertexCount = rayCount + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[rayCount * 3];
        vertices[0] = Vector3.zero;

        float angleStep = 360f / rayCount;

        for (int i = 0; i <= rayCount; i++)
        {
            float angle = angleStep * i;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                direction,
                radius,
                obstacleLayer
            );

            Vector3 worldPoint;
            if (hit.collider != null)
            {
                worldPoint = hit.point;
            }
            else
            {
                worldPoint = (Vector2)transform.position + direction * radius;
            }

            vertices[i + 1] = transform.InverseTransformPoint(worldPoint);
        }
        for (int i = 0; i < rayCount; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        lightMesh.Clear();
        lightMesh.vertices = vertices;
        lightMesh.triangles = triangles;
        lightMesh.RecalculateBounds();
    }
}