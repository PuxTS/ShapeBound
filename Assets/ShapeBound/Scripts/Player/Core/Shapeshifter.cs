using UnityEngine;

public enum ShapeForm { Circle = 1, Box = 2, Spike = 3 }

public class Shapeshifter : MonoBehaviour
{
    [Header("Parent Colliders")]
    [SerializeField] private CircleCollider2D circleCollider;
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private BoxCollider2D spikeCollider;

    [Header("Child Visual GameObjects")]
    [SerializeField] private GameObject circleVisual;
    [SerializeField] private GameObject boxVisual;
    [SerializeField] private GameObject spikeVisual;

    [Header("UI Reference")]
    [SerializeField] private ShapeFormUI shapeFormUI; // Added reference

    [Header("State")]
    [SerializeField] private ShapeForm currentForm = ShapeForm.Circle;
    public ShapeForm CurrentForm => currentForm;
    public bool CanShift { get; set; } = true;

    private void Start()
    {
        // If UI reference isn't assigned, auto-find it in the scene
        if (shapeFormUI == null)
            shapeFormUI = FindAnyObjectByType<ShapeFormUI>();

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
        currentForm = form;

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

        // Update the UI representation
        if (shapeFormUI != null)
        {
            shapeFormUI.UpdateActiveShape(CurrentForm);
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