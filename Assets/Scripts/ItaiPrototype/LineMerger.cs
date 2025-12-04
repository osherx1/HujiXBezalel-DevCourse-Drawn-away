using System.Collections.Generic;
using UnityEngine;

namespace ItaiPrototype
{
    public class LineMerger : MonoBehaviour
    {
        [SerializeField] private Transform lineRoot;

        public void CreateNewPhysicalObject()
        {
            if (lineRoot.childCount == 0) return;

            // --- STEP 1: CALCULATE MASS & CENTER ---
            Vector3 weightedPositionSum = Vector3.zero;
            float totalMass = 0f;
            List<Transform> linesToMove = new List<Transform>();

            foreach (Transform child in lineRoot)
            {
                linesToMove.Add(child);
                Rigidbody2D childRb = child.GetComponent<Rigidbody2D>();
                float mass = (childRb != null) ? childRb.mass : 1f;
                
                totalMass += mass;
                weightedPositionSum += child.position * mass;
            }

            Vector3 centerOfMass = weightedPositionSum / totalMass;

            // --- STEP 2: CREATE THE NEW PARENT ---
            GameObject newParentObj = new GameObject("Physical_Drawing_" + Time.frameCount)
            {
                transform =
                {
                    position = centerOfMass
                }
            };

            // Add the custom gravity controller
            CompositeGravityBody gravityController = newParentObj.AddComponent<CompositeGravityBody>();
            
            // --- STEP 3: TRANSFER CHILDREN & DATA ---
            foreach (Transform line in linesToMove)
            {
                // Capture the data BEFORE we destroy the child Rigidbody
                Rigidbody2D lineRb = line.GetComponent<Rigidbody2D>();
                float m = (lineRb != null) ? lineRb.mass : 1f;
                float g = (lineRb != null) ? lineRb.gravityScale : 1f;

                // Parent the line
                line.SetParent(newParentObj.transform);

                // Add the data to our new controller
                // Note: line.localPosition is now correct relative to the new parent
                gravityController.gravityPoints.Add(new CompositeGravityBody.GravityPoint 
                {
                    localPosition = line.localPosition,
                    mass = m,
                    gravityScale = g
                });

                // Cleanup
                if (lineRb != null) Destroy(lineRb);
            }

            // --- STEP 4: CONFIGURE MAIN RIGIDBODY ---
            Rigidbody2D rootRb = newParentObj.AddComponent<Rigidbody2D>();
            rootRb.mass = totalMass; 
            
            // Note: rootRb.gravityScale will be set to 0 automatically 
            // by the CompositeGravityBody Start() method.
        }
    }
}