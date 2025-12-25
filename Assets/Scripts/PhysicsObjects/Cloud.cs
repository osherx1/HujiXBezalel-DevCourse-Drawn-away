using UnityEngine;

// Required to access the Line script

namespace PhysicsObjects
{

    public class Cloud : MonoBehaviour
    {
        /*
        [Tooltip("Assign the Line component here, or leave empty if it's on the same GameObject")]
        [SerializeField] private Line targetLine;
        */


        // --- Player Carrying Logic ---
        
        void OnCollisionEnter2D(Collision2D collision)
        {
            // Parent the player to the cloud so they move together
            if (collision.gameObject.CompareTag("Player"))
            {
                collision.transform.SetParent(this.transform);
            }
        }

        void OnCollisionExit2D(Collision2D collision)
        {
            // Unparent the player when they leave the cloud
            if (collision.gameObject.CompareTag("Player"))
            {
                collision.transform.SetParent(null);
            }
        }


    }
}