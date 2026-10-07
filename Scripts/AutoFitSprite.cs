using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AutoFitSprite : MonoBehaviour
{
    public enum FitMode
    {
        FitToCollider,
        FitToSize
    }

    [Header("Режим подгонки")]
    [SerializeField] private FitMode mode = FitMode.FitToCollider;

    [Header("Для режима FitToSize")]
    [SerializeField] private float targetWidth = 1f;
    [SerializeField] private float targetHeight = 1f;

    private void Start()
    {
        Fit();
    }

    private void OnValidate()
    {
        if (GetComponent<SpriteRenderer>() != null)
        {
            Fit();
        }
    }

    public void Fit()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer.sprite == null)
        {
            return;
        }

        Vector2 spriteSize = renderer.sprite.bounds.size;
        Vector2 targetSize;

        if (mode == FitMode.FitToCollider)
        {
            BoxCollider2D box = GetComponent<BoxCollider2D>();
            targetSize = box != null ? box.size : new Vector2(targetWidth, targetHeight);
        }
        else
        {
            targetSize = new Vector2(targetWidth, targetHeight);
        }

        float scaleX = targetSize.x / spriteSize.x;
        float scaleY = targetSize.y / spriteSize.y;

        transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }
}