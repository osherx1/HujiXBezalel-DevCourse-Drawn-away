using UnityEngine;

namespace Drawing.LineControl
{
    [RequireComponent(typeof(GlueHandler))]
    public class GlueDebugger : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [Tooltip("Draws a line from the glue center to the connection point.")]
        [SerializeField] private bool showConnections = true;
        
        [Tooltip("Draws a vector representing the physical force trying to break the joint.")]
        [SerializeField] private bool showStressLevels = true;

        [Header("Colors")]
        [SerializeField] private Color anchorColor = Color.green;
        [SerializeField] private Color connectionLineColor = Color.cyan;
        [SerializeField] private Color highStressColor = Color.red;
        [SerializeField] private Color lowStressColor = Color.green;

        [Header("Scale")]
        [SerializeField] private float stressLineScale = 0.01f; // Visual scaler for force lines

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Get all joints created by the GlueHandler on this object
            FixedJoint2D[] joints = GetComponents<FixedJoint2D>();

            foreach (var joint in joints)
            {
                if (joint == null) continue;

                // 1. Calculate World Positions of the Anchor Points
                Vector2 myAnchorWorld = transform.TransformPoint(joint.anchor);
                Vector2 connectedAnchorWorld;

                if (joint.connectedBody != null)
                {
                    // If connected to a Rigidbody (Rope), point is local to that body
                    connectedAnchorWorld = joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);
                }
                else
                {
                    // If connected to null (Static World), point is already World Space
                    connectedAnchorWorld = joint.connectedAnchor;
                }

                // 2. Visual: Draw the Connection Bond
                if (showConnections)
                {
                    Gizmos.color = anchorColor;
                    Gizmos.DrawWireSphere(myAnchorWorld, 0.05f); // The Glue Spot
                    Gizmos.DrawWireSphere(connectedAnchorWorld, 0.02f); // The Rope Spot

                    Gizmos.color = connectionLineColor;
                    Gizmos.DrawLine(myAnchorWorld, connectedAnchorWorld);
                }

                // 3. Visual: Draw Stress (Reaction Force)
                // This shows you WHY a joint breaks. If this line gets huge/red, your BreakForce is too low.
                if (showStressLevels)
                {
                    Vector2 reactionForce = joint.reactionForce;
                    float forceMagnitude = reactionForce.magnitude;

                    // Color based on how close we are to breaking
                    // Note: If breakForce is Infinity, we just use Green.
                    float breakThreshold = joint.breakForce;
                    
                    if (float.IsInfinity(breakThreshold))
                    {
                        Gizmos.color = lowStressColor;
                    }
                    else
                    {
                        float stressFactor = Mathf.Clamp01(forceMagnitude / breakThreshold);
                        Gizmos.color = Color.Lerp(lowStressColor, highStressColor, stressFactor);
                    }

                    // Draw the force vector emanating from the anchor
                    Gizmos.DrawRay(myAnchorWorld, reactionForce * stressLineScale);
                }
            }
        }
    }
}