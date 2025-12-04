using UnityEngine;

namespace ItaiPrototype.Scenario3
{
    public class DirtChunk : MonoBehaviour
    {
        public DirtManager manager;
        private bool hasBeenRemoved = false;

        // This checks if the dirt has been lifted ABOVE the ground level
        void Update()
        {
            if (hasBeenRemoved) return;

            // Assuming Grave Origin Y is the surface level. 
            // If dirt goes 1 unit above origin, it counts as "dug out".
            if (transform.position.y > manager.transform.position.y + 1.0f)
            {
                hasBeenRemoved = true;
                manager.ReportDirtRemoved();
            
                // Optional: Destroy or deactivate to save performance
                // Destroy(gameObject, 1f); 
            }
        }
    }
}