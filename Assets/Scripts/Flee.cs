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

    [Header("Evasión de Obstáculos")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float obstacleDetectionRange = 2f;
    [SerializeField] private float obstacleRaySpread = 30f;
    [SerializeField] private int obstacleRayCount = 5;

    [Header("Evasión de Paredes")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallDetectionRange = 1.5f;

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
        
        Vector2 obstacleAvoidance = ObstacleAvoidance.GetAvoidanceForce(
            transform.position, rb.linearVelocity, moveSpeed, obstacleLayer, 
            obstacleDetectionRange, obstacleRaySpread, obstacleRayCount);
        
        Vector2 wallAvoidance = ObstacleAvoidance.GetWallAvoidanceForce(
            transform.position, rb.linearVelocity, moveSpeed, wallLayer, 
            wallDetectionRange);

        Vector2 finalVelocity = desired + obstacleAvoidance + wallAvoidance;
        finalVelocity = finalVelocity.normalized * moveSpeed;
        
        rb.linearVelocity = finalVelocity;
    }

    void Wander()
    {
        timer += Time.deltaTime;

        if (timer >= changeDirectionInterval)
        {
            PickWanderDirection();
            timer = 0f;
        }

        Vector2 desiredVelocity = currentDirection * moveSpeed;
        
        Vector2 obstacleAvoidance = ObstacleAvoidance.GetAvoidanceForce(
            transform.position, rb.linearVelocity, moveSpeed, obstacleLayer, 
            obstacleDetectionRange, obstacleRaySpread, obstacleRayCount);
        
        Vector2 wallAvoidance = ObstacleAvoidance.GetWallAvoidanceForce(
            transform.position, rb.linearVelocity, moveSpeed, wallLayer, 
            wallDetectionRange);

        Vector2 finalVelocity = desiredVelocity + obstacleAvoidance + wallAvoidance;
        finalVelocity = finalVelocity.normalized * moveSpeed;
        
        rb.linearVelocity = finalVelocity;
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

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = Color.red;
        Vector2 forward = rb.linearVelocity.normalized;
        if (forward == Vector2.zero) forward = Vector2.up;
        
        float halfSpread = obstacleRaySpread * 0.5f;
        float step = obstacleRayCount > 1 ? obstacleRaySpread / (obstacleRayCount - 1) : 0f;
        
        for (int i = 0; i < obstacleRayCount; i++)
        {
            float angle = -halfSpread + i * step;
            Vector2 rayDir = Quaternion.Euler(0, 0, angle) * forward;
            Gizmos.DrawRay(transform.position, rayDir * obstacleDetectionRange);
        }
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, forward * wallDetectionRange);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, 0, 45) * forward * wallDetectionRange);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, 0, -45) * forward * wallDetectionRange);
    }
}