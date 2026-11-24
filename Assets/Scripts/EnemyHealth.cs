
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 10;

    private int currentHealth;
    private bool isDead = false;

    private EnemyAI enemyAI;
    private Rigidbody rb;
    private Collider enemyCollider;

    private Animator animator;

    [Header("Testing")]
    public int testDamage = 3;

    void Start()
    {
        currentHealth = maxHealth;

        enemyAI = GetComponent<EnemyAI>();
        rb = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();

        animator = GetComponentInChildren<Animator>();

        Debug.Log("Enemy HP: " + currentHealth + "/" + maxHealth);
    }

    void Update()
    {
        // Temporary testing key
        if (Input.GetKeyDown(KeyCode.J))
        {
            TakeDamage(testDamage);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log("Enemy HP: " + currentHealth + "/" + maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        Debug.Log("ENEMY DIED!");

        if (animator != null)
            animator.SetTrigger("Death");

        if (enemyAI != null)
            enemyAI.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (enemyCollider != null)
            enemyCollider.enabled = false;
    }

    public bool IsDead()
    {
        return isDead;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }
}
