
using UnityEngine;


public class EnemyAI : MonoBehaviour
{
    private Animator animator;
    private float animationSpeed;

    public enum EnemyState
    {
        Roaming,
        Chasing,
        Returning,
        PreparingAttack,
        Attacking,
        Recovering
    }

    [Header("References")]
    public Transform player;

    [Header("Roaming")]
    public float roamRadius = 5f;
    public float roamSpeed = 1.5f;
    public float waitTime = 2f;

    [Header("Detection")]
    public float detectionRange = 10f;
    public float chaseSpeed = 3f;
    public float attackRange = 2.5f;

    [Header("Attack")]
    public int attackDamage = 4;
    public float preparationTime = 1f;
    public float attackDuration = 0.3f;
    public float recoveryTime = 1.5f;

    [Header("Attack Hitbox")]
    public Vector3 hitboxSize = new Vector3(1.5f, 1.5f, 1.5f);
    public float hitboxForwardDistance = 1.2f;
    public float hitboxHeight = 0.8f;

    [Header("Rotation")]
    public float rotationSpeed = 5f;

    private Rigidbody rb;

    private Vector3 startingPosition;
    private Vector3 roamTarget;

    private float waitTimer;
    private float stateTimer;

    private bool isWaiting;
    private bool hasHitPlayer;

    private EnemyState currentState = EnemyState.Roaming;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        animator = GetComponentInChildren<Animator>();

        startingPosition = transform.position;

        ChooseNewRoamTarget();
    }

    void Update()
    {
        if (player == null)
            return;

        float playerDistance = GetPlayerDistance();

        switch (currentState)
        {
            case EnemyState.Roaming:

                if (playerDistance <= detectionRange)
                {
                    currentState = EnemyState.Chasing;
                    isWaiting = false;
                }
                else
                {
                    UpdateRoaming();
                }

                break;

            case EnemyState.Chasing:

                if (playerDistance > detectionRange)
                {
                    currentState = EnemyState.Returning;
                }
                else if (playerDistance <= attackRange)
                {
                    StartPreparingAttack();
                }

                break;

            case EnemyState.PreparingAttack:

                FacePlayer();

                if (playerDistance > detectionRange)
                {
                    currentState = EnemyState.Returning;
                }
                else if (playerDistance > attackRange)
                {
                    currentState = EnemyState.Chasing;
                }
                else
                {
                    stateTimer -= Time.deltaTime;

                    if (stateTimer <= 0f)
                    {
                        StartAttack();
                    }
                }

                break;

            case EnemyState.Attacking:

                stateTimer -= Time.deltaTime;

                FacePlayer();
                CheckAttackHitbox();

                if (stateTimer <= 0f)
                {
                    currentState = EnemyState.Recovering;
                    stateTimer = recoveryTime;
                }

                break;

            case EnemyState.Recovering:

                stateTimer -= Time.deltaTime;

                if (stateTimer <= 0f)
                {
                    if (playerDistance <= detectionRange)
                    {
                        currentState = EnemyState.Chasing;
                    }
                    else
                    {
                        currentState = EnemyState.Returning;
                    }
                }

                break;

            case EnemyState.Returning:

                float homeDistance = Vector3.Distance(
                    transform.position,
                    startingPosition
                );

                if (homeDistance < 0.3f)
                {
                    currentState = EnemyState.Roaming;
                    ChooseNewRoamTarget();
                }

                break;
        }
    }

    void FixedUpdate()
    {
        animationSpeed = 0f;

        switch (currentState)
        {
            case EnemyState.Roaming:

                if (!isWaiting)
                    MoveTowards(roamTarget, roamSpeed);

                break;

            case EnemyState.Chasing:

                if (player != null)
                    MoveTowards(player.position, chaseSpeed, attackRange);

                break;

            case EnemyState.Returning:

                MoveTowards(startingPosition, chaseSpeed);

                break;
        }

        UpdateAnimation();
    }

    void UpdateRoaming()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                isWaiting = false;
                ChooseNewRoamTarget();
            }

            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            roamTarget
        );

        if (distance < 0.3f)
        {
            isWaiting = true;
            waitTimer = waitTime;
        }
    }

    void MoveTowards(Vector3 target, float speed, float stoppingDistance = 0f)
    {
        Vector3 direction = target - rb.position;
        direction.y = 0f;

        if (direction.magnitude <= stoppingDistance)
            return;

        direction.Normalize();

        animationSpeed = speed;

        rb.MovePosition(
            rb.position + direction * speed * Time.fixedDeltaTime
        );

        RotateTowards(direction);
    }

    void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        rb.MoveRotation(
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            )
        );
    }

    void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        rb.MoveRotation(
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            )
        );
    }

    void StartPreparingAttack()
    {
        currentState = EnemyState.PreparingAttack;
        stateTimer = preparationTime;
    }

    void StartAttack()
    {
        currentState = EnemyState.Attacking;
        stateTimer = attackDuration;
        hasHitPlayer = false;

        if (animator != null)
            animator.SetTrigger("Attack");
    }

    void CheckAttackHitbox()
    {
        if (hasHitPlayer)
            return;

        Vector3 hitboxCenter =
            transform.position +
            transform.forward * hitboxForwardDistance +
            Vector3.up * hitboxHeight;

        Collider[] hits = Physics.OverlapBox(
            hitboxCenter,
            hitboxSize * 0.5f,
            transform.rotation
        );

        foreach (Collider hit in hits)
        {
            PlayerHealth health = hit.GetComponentInParent<PlayerHealth>();

            if (health != null && !health.IsDead())
            {
                health.TakeDamage(attackDamage);
                hasHitPlayer = true;

                Debug.Log("Enemy hit player!");
                break;
            }
        }
    }

    float GetPlayerDistance()
    {
        Vector3 difference = player.position - transform.position;
        difference.y = 0f;

        return difference.magnitude;
    }

    void ChooseNewRoamTarget()
    {
        Vector2 randomPoint = Random.insideUnitCircle * roamRadius;

        roamTarget = startingPosition + new Vector3(
            randomPoint.x,
            0f,
            randomPoint.y
        );
    }

    void UpdateAnimation()
    {
        if (animator == null)
            return;

        animator.SetFloat("Speed", animationSpeed);
    }
}
