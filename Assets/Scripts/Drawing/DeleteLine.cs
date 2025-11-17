using System;
using UnityEngine;

namespace Drawing
{
    public class DeleteLine : MonoBehaviour
    {
        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.CompareTag("Line"))
            {
                Destroy(other.gameObject);
            }
        }
    }
}
