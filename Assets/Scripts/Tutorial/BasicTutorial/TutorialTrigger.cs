using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [Tooltip("Reference to the Tutorial Manager in the scene.")]
    [SerializeField] private NewTutorialManager tutorialManager;
    [Tooltip("The tag of the player object.")]
    [SerializeField] private string playerTag = "Player";

    private bool _hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasTriggered) return;

        if (other.CompareTag(playerTag))
        {
            _hasTriggered = true;
            
            if (tutorialManager != null)
            {
                tutorialManager.StartLevel2Instruction();
            }
            
            // Optional: Destroy this trigger so it doesn't clutter the scene
            // Destroy(gameObject); 
            // Or just disable collider if you prefer:
            GetComponent<Collider2D>().enabled = false;
        }
    }
}