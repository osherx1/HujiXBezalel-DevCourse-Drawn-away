using UnityEngine;

namespace Drawing.LineControl
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
