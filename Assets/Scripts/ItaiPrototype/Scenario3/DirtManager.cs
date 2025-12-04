using ItaiPrototype.Utilities;
using UnityEngine;

namespace ItaiPrototype.Scenario3
{
    public class DirtManager : MonoBehaviour
    {
        [Header("Grave Settings")]
        [SerializeField] private GameObject dirtPrefab; // A small brown square sprite
        [SerializeField] private int width = 10;
        [SerializeField] private int depth = 8;
        [SerializeField] private Transform graveOrigin;

        [Header("Win Condition")]
        [SerializeField] private int dirtToRemove = 40; // How many chunks to clear?
        private int removedDirtCount = 0;
        private ScenarioManager scenarioManager;

        void Start()
        {
            scenarioManager = FindObjectOfType<ScenarioManager>();
            SpawnGrave();
        }

        void SpawnGrave()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < depth; y++)
                {
                    Vector3 pos = graveOrigin.position + new Vector3(x * 0.5f, -y * 0.5f, 0);
                    GameObject dirt = Instantiate(dirtPrefab, pos, Quaternion.identity, transform);
                
                    // Track when this specific dirt chunk leaves the hole
                    DirtChunk chunkScript = dirt.GetComponent<DirtChunk>();
                    chunkScript.manager = this;
                }
            }
        }

        public void ReportDirtRemoved()
        {
            removedDirtCount++;
            // Optional: Update UI Bar
        
            if (removedDirtCount >= dirtToRemove)
            {
                Debug.Log("Coffin Exposed!");
                if (scenarioManager != null) scenarioManager.Win();
            }
        }
    }
}