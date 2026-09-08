using UnityEngine;

public class Seek : MonoBehaviour
{
    [Header("Configuración Seek")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float arriveRange = 1f;
    [SerializeField] private float wanderRange = 2f;
    [SerializeField] private float changeDirectionInterval = 2f;
    [SerializeField] private float giveUpRange = 12f;

    private Rigidbody2D rb;
    private Transform player;
    private Vector2 currentDirection;
    private float timer;
    private Vector2 startPosition;
    private bool isChasing;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError($"[Seek] NO hay Rigidbody2D en: {gameObject.name}");
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

        float distPlayerToStart = Vector2.Distance(player.position, startPosition);

        if (!isChasing && distPlayerToStart <= detectionRange)
        {
            isChasing = true;
        }
        else if (isChasing && distPlayerToStart > giveUpRange)
        {
            isChasing = false;
        }

        if (isChasing)
        {
            SeekPlayer();
        }
        else
        {
            Wander();
        }
    }

    void SeekPlayer()
    {
        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float dist = toPlayer.magnitude;

        if (dist <= arriveRange)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 desired = toPlayer.normalized * moveSpeed;
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