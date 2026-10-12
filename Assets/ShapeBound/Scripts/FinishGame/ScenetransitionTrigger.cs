using UnityEngine;
using UnityEngine.SceneManagement;

// When the player walks into the trigger, it loads the scene.
// Scenes must be listed in File > Build Settings.
[RequireComponent(typeof(BoxCollider2D))]
public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("Scene To Load")]
    [SerializeField] private string sceneName;
    [Tooltip("Tick to load by Build Settings number instead of by name (first scene = 0).")]
    [SerializeField] private bool useBuildIndex = false;
    [SerializeField] private int buildIndex = 2;

    private bool hasTriggered;

    // Runs when the component is first added in the editor
    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;

        // The player's colliders sit on the same object as PlayerController
        if (other.GetComponentInParent<PlayerController>() == null) return;

        hasTriggered = true;

        AsyncOperation operation = useBuildIndex
            ? SceneManager.LoadSceneAsync(buildIndex)
            : SceneManager.LoadSceneAsync(sceneName);

        if (operation == null) hasTriggered = false;
    }
}