
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 10;

    private int currentHealth;

    private Animator animator;
    private PlayerMovement playerMovement;
    private Rigidbody rb;

    private bool isDead = false;

    [Header("Testing")]
    public int testDamage = 3;

    private ChickenTransformation chickenTransformation;

    void Start()
    {
        currentHealth = maxHealth;

        animator = GetComponentInChildren<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody>();

        Debug.Log("Player HP: " + currentHealth + "/" + maxHealth);

        chickenTransformation = GetComponent<ChickenTransformation>();
    }

    void Update()
    {
        // Press K to damage yourself
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(testDamage);
        }
    }

    public void TakeDamage(int damage)
    {

        if (isDead)
            return;

        if (chickenTransformation != null && chickenTransformation.IsChicken())
        {
            damage = 1;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log("Player HP: " + currentHealth + "/" + maxHealth);

        if (currentHealth <= 0)
        {
            if (chickenTransformation != null &&
                chickenTransformation.IsChicken())
            {
                chickenTransformation.ReturnToHuman();
            }

            Die();
        }
        else
        {
            // Play damaged animation
            if (animator != null)
            {
                animator.SetTrigger("Damaged");
            }
        }
    }

    void Die()
    {
        isDead = true;

        Debug.Log("PLAYER DIED!");

        // Stop player movement
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // Stop Rigidbody movement
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        // Play death animation
        if (animator != null)
        {
            animator.SetTrigger("Dead");
        }
    }

    public void Heal(int amount)
    {
        if (isDead)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        Debug.Log("Player healed! HP: " + currentHealth + "/" + maxHealth);
    }

    public void HealByPercentage(float percentage)
    {
        int healAmount = Mathf.CeilToInt(maxHealth * percentage);
        Heal(healAmount);
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public bool IsDead()
    {
        return isDead;
    }
}
