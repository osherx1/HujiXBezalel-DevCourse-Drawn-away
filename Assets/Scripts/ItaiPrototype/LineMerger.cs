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

            // --- STEP 1: CALCULATE MASS & CENTER (Updated) ---
            Vector3 weightedPositionSum = Vector3.zero;
            float totalMass = 0f;
            List<Transform> linesToMove = new List<Transform>();

            // New list to store the exact world center of mass for each line
            List<Vector2> linesWorldCoM = new List<Vector2>(); 

            foreach (Transform child in lineRoot)
            {
                linesToMove.Add(child);
                Rigidbody2D childRb = child.GetComponent<Rigidbody2D>();
                
                float mass = (childRb != null) ? childRb.mass : 1f;
                
                // FIX: Use the calculated Physics Center, not the Transform Pivot
                Vector2 trueCenter = (childRb != null) ? childRb.worldCenterOfMass : (Vector2)child.position;
                
                linesWorldCoM.Add(trueCenter); // Save this for later

                totalMass += mass;
                weightedPositionSum += (Vector3)trueCenter * mass;
            }

            Vector3 finalCenterOfMass = weightedPositionSum / totalMass;

            // --- STEP 2: CREATE THE NEW PARENT ---
            GameObject newParentObj = new GameObject("Physical_Drawing_" + Time.frameCount);
            newParentObj.transform.position = finalCenterOfMass;
            
            // Optional: Set Layer
            // newParentObj.layer = LayerMask.NameToLayer("Objects");

            CompositeGravityBody gravityController = newParentObj.AddComponent<CompositeGravityBody>();
            
            // --- STEP 3: TRANSFER CHILDREN & DATA ---
            for (int i = 0; i < linesToMove.Count; i++)
            {
                Transform line = linesToMove[i];
                Vector2 worldCoM = linesWorldCoM[i]; // Retrieve the stored center

                Rigidbody2D lineRb = line.GetComponent<Rigidbody2D>();
                float m = (lineRb != null) ? lineRb.mass : 1f;
                float g = (lineRb != null) ? lineRb.gravityScale : 1f;

                // Parent the line
                line.SetParent(newParentObj.transform);

                // FIX: Calculate where that Center of Mass is relative to the NEW parent
                // We use InverseTransformPoint to convert "World Center" to "Local Parent Space"
                Vector3 localCoM = newParentObj.transform.InverseTransformPoint(worldCoM);

                gravityController.gravityPoints.Add(new CompositeGravityBody.GravityPoint 
                {
                    // We now store the physical center, not the visual pivot
                    localPosition = localCoM, 
                    mass = m,
                    gravityScale = g
                });

                if (lineRb != null) Destroy(lineRb);
            }

            // --- STEP 4: CONFIGURE MAIN RIGIDBODY ---
            Rigidbody2D rootRb = newParentObj.AddComponent<Rigidbody2D>();
            rootRb.mass = totalMass; 
        }
    }
}