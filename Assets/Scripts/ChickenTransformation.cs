
using UnityEngine;
using System.Collections;

public class ChickenTransformation : MonoBehaviour
{
    [Header("Character Models")]
    public GameObject humanModel;
    public GameObject chickenModel;

    [Header("Transformation")]
    public float chickenDuration = 10f;

    [Header("Chicken Combat")]
    public int chickenAttackDamage = 5;

    public float attackHitDelay = 0.25f;
    public float attackCooldown = 0.8f;

    public Vector3 attackHitboxSize = new Vector3(1.5f, 1.5f, 1.5f);
    public float attackForwardDistance = 1.2f;
    public float attackHeight = 0.8f;

    private Animator chickenAnimator;
    private bool isAttacking = false;

    private bool isChicken = false;
    private Coroutine transformationCoroutine;

    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        chickenAnimator = chickenModel.GetComponentInChildren<Animator>(true);
    }

    void Update()
    {
        if (!isChicken || isAttacking)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(PerformChickenAttack());
        }
    }

    public bool IsChicken()
    {
        return isChicken;
    }

    public void TransformIntoChicken()
    {
        if (isChicken)
            return;

        if (transformationCoroutine != null)
            StopCoroutine(transformationCoroutine);

        transformationCoroutine = StartCoroutine(ChickenTimer());
    }

    IEnumerator ChickenTimer()
    {
        isChicken = true;

        humanModel.SetActive(false);
        chickenModel.SetActive(true);

        playerMovement.SetCharacterModel(chickenModel.transform);

        Debug.Log("CHICKEN MODE ACTIVATED!");

        yield return new WaitForSeconds(chickenDuration);

        ReturnToHuman();
    }

    public void ReturnToHuman()
    {
        isChicken = false;

        humanModel.SetActive(true);
        chickenModel.SetActive(false);

        playerMovement.SetCharacterModel(humanModel.transform);

        Debug.Log("Returned to human form!");

        transformationCoroutine = null;
    }

    IEnumerator PerformChickenAttack()
    {
        isAttacking = true;

        if (chickenAnimator != null)
        {
            chickenAnimator.SetTrigger("Peck");
        }

        // Wait until the chicken actually pecks
        yield return new WaitForSeconds(attackHitDelay);

        if (isChicken)
        {
            AttackEnemiesInFront();
        }

        // Wait for the remaining cooldown
        yield return new WaitForSeconds(
            Mathf.Max(0f, attackCooldown - attackHitDelay)
        );

        isAttacking = false;
    }

    void AttackEnemiesInFront()
    {
        Vector3 hitboxCenter =
            chickenModel.transform.position +
            chickenModel.transform.forward * attackForwardDistance +
            Vector3.up * attackHeight;

        Collider[] hits = Physics.OverlapBox(
            hitboxCenter,
            attackHitboxSize * 0.5f,
            chickenModel.transform.rotation
        );

        System.Collections.Generic.HashSet<EnemyHealth> damagedEnemies =
            new System.Collections.Generic.HashSet<EnemyHealth>();

        foreach (Collider hit in hits)
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();

            if (enemy != null && !enemy.IsDead() && damagedEnemies.Add(enemy))
            {
                enemy.TakeDamage(chickenAttackDamage);

                Debug.Log("Chicken pecked enemy for " + chickenAttackDamage + " damage!");
            }
        }
    }
}
