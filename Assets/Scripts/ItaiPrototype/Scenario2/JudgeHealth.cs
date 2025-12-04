using ItaiPrototype.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace ItaiPrototype.Scenario2
{
    public class JudgeHealth : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private Image healthBarFill;
        [SerializeField] private float damageMultiplier = 1.0f;
        [SerializeField] private float minDamageThreshold = 0.1f; // Extremely low for testing

        private float currentHealth;
        private ScenarioManager scenarioManager;

        void Start()
        {
            scenarioManager = FindObjectOfType<ScenarioManager>();
            currentHealth = maxHealth;
            UpdateHealthBar();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // DEBUG LOGGING
            Debug.Log($"Judge collided with: {collision.gameObject.name} (Tag: {collision.gameObject.tag})");

            // Check if Parent Rigidbody exists and has the tag
            Rigidbody2D otherRb = collision.collider.attachedRigidbody;
        
            if (otherRb == null)
            {
                Debug.Log(">> Collision ignored: No Rigidbody found on object.");
                return;
            }

            Debug.Log($">> Parent Rigidbody Tag: {otherRb.tag}");

            if (otherRb.CompareTag("Player"))
            {
                float impactForce = collision.relativeVelocity.magnitude;
                float weaponMass = otherRb.mass;
                float damage = impactForce * weaponMass * damageMultiplier;

                Debug.Log($">> HIT! Force: {impactForce:F2}, Mass: {weaponMass:F2}, DAMAGE: {damage:F2}");

                if (damage > minDamageThreshold)
                {
                    TakeDamage(damage);
                }
                else
                {
                    Debug.Log($">> Damage too low (Threshold: {minDamageThreshold})");
                }
            }
        }

        private void TakeDamage(float amount)
        {
            currentHealth -= amount;
            if (currentHealth < 0) currentHealth = 0;
            UpdateHealthBar();
            if (currentHealth <= 0) Die();
        }

        private void UpdateHealthBar()
        {
            if (healthBarFill != null) healthBarFill.fillAmount = currentHealth / maxHealth;
        }

        private void Die()
        {
            Debug.Log("Judge Defeated!");
            gameObject.SetActive(false); 
            if (scenarioManager != null) scenarioManager.Win();
        }
    }
}