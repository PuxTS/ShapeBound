using UnityEngine;

public class WallBreaker : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Shapeshifter shapeshifter = collision.collider.GetComponentInParent<Shapeshifter>();
        if (shapeshifter != null && shapeshifter.CurrentForm == ShapeForm.Box)
        {
            Destroy(gameObject);
        }

    }
}
