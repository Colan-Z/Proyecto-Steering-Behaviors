using UnityEngine;

public class Flee : MonoBehaviour
{
    [Header("Configuración Flee")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float fleeRange = 10f;
    [SerializeField] private float wanderRange = 3f;
    [SerializeField] private float changeDirectionInterval = 2f;
    [SerializeField] private float safeDistance = 12f;

    private Rigidbody2D rb;
    private Transform player;
    private Vector2 currentDirection;
    private float timer;
    private Vector2 startPosition;
    private bool isFleeing;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError($"[Flee] NO hay Rigidbody2D en: {gameObject.name}");
            enabled = false;
            return;
        }

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        startPosition = transform.position;
        PickWanderDirection();
    }

    void Update()
    {
        if (player == null) return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        if (!isFleeing && distToPlayer <= detectionRange)
        {
            isFleeing = true;
        }
        else if (isFleeing && distToPlayer >= safeDistance)
        {
            isFleeing = false;
        }

        if (isFleeing)
        {
            FleeFromPlayer();
        }
        else
        {
            Wander();
        }
    }

    void FleeFromPlayer()
    {
        Vector2 awayFromPlayer = (Vector2)transform.position - (Vector2)player.position;
        
        if (awayFromPlayer.magnitude < 0.1f)
        {
            awayFromPlayer = Random.insideUnitCircle.normalized;
        }

        Vector2 desired = awayFromPlayer.normalized * moveSpeed;
        rb.linearVelocity = desired;
    }

    void Wander()
    {
        timer += Time.deltaTime;

        if (timer >= changeDirectionInterval)
        {
            PickWanderDirection();
            timer = 0f;
        }

        rb.linearVelocity = currentDirection * moveSpeed;
    }

    void PickWanderDirection()
    {
        Vector2 randomOffset = Random.insideUnitCircle * wanderRange;
        Vector2 targetPos = startPosition + randomOffset;
        currentDirection = (targetPos - rb.position).normalized;

        if (currentDirection == Vector2.zero)
        {
            currentDirection = Random.insideUnitCircle.normalized;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PickWanderDirection();
    }
}