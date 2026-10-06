using UnityEngine;

public enum ShapeForm { Circle = 1, Box = 2, Spike = 3 }

public class Shapeshifter : MonoBehaviour
{
    [Header("Parent Colliders")]
    [SerializeField] private CircleCollider2D circleCollider;
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private PolygonCollider2D spikeCollider;

    [Header("Child Visual GameObjects")]
    [SerializeField] private GameObject circleVisual;
    [SerializeField] private GameObject boxVisual;
    [SerializeField] private GameObject spikeVisual;

    public ShapeForm CurrentForm { get; private set; } = ShapeForm.Circle;
    public bool CanShift { get; set; } = true;

    private void Start()
    {
        // Set default initial shape on start (Circle)
        SetShape(1);
    }

    public void SetShape(int shapeIndex)
    {
        if (!CanShift) return;

        SetShapeInternal((ShapeForm)shapeIndex);
    }

    // Overload 2: Accepts ShapeForm enum directly
    public void SetShape(ShapeForm newForm)
    {
        if (!CanShift) return;

        SetShapeInternal(newForm);
    }

    private void SetShapeInternal(ShapeForm form)
    {
        CurrentForm = form;

        // Disable all physics colliders on root
        if (circleCollider != null) circleCollider.enabled = false;
        if (boxCollider != null) boxCollider.enabled = false;
        if (spikeCollider != null) spikeCollider.enabled = false;

        // Hide all visual child objects
        if (circleVisual != null) circleVisual.SetActive(false);
        if (boxVisual != null) boxVisual.SetActive(false);
        if (spikeVisual != null) spikeVisual.SetActive(false);

        // Enable matching collider & visual object
        switch (CurrentForm)
        {
            case ShapeForm.Circle:
                if (circleCollider != null) circleCollider.enabled = true;
                if (circleVisual != null) circleVisual.SetActive(true);
                break;
            case ShapeForm.Box:
                if (boxCollider != null) boxCollider.enabled = true;
                if (boxVisual != null) boxVisual.SetActive(true);
                break;
            case ShapeForm.Spike:
                if (spikeCollider != null) spikeCollider.enabled = true;
                if (spikeVisual != null) spikeVisual.SetActive(true);
                break;
        }
    }

    // Returns the currently active collider component based on the active form
    public Collider2D GetActiveCollider()
    {
        return CurrentForm switch
        {
            ShapeForm.Circle => circleCollider,
            ShapeForm.Box => boxCollider,
            ShapeForm.Spike => spikeCollider,
            _ => null
        };
    }

    public float GetBounciness()
    {
        return CurrentForm switch
        {
            ShapeForm.Circle => 0.85f,
            ShapeForm.Box => 0.0f,
            ShapeForm.Spike => 0.1f,
            _ => 0.5f
        };
    }
}