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

    [Header("Evasión de Obstáculos (Chaser)")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float obstacleDetectionRange = 3f;
    [SerializeField] private float obstacleRaySpread = 60f;
    [SerializeField] private int obstacleRayCount = 7;
    [SerializeField] private float avoidancePriority = 2f;

    [Header("Evasión de Paredes")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallDetectionRange = 2f;

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
        
        Vector2 obstacleAvoidance = ObstacleAvoidance.GetAvoidanceForce(
            transform.position, rb.linearVelocity, moveSpeed, obstacleLayer, 
            obstacleDetectionRange, obstacleRaySpread, obstacleRayCount);
        
        Vector2 wallAvoidance = ObstacleAvoidance.GetWallAvoidanceForce(
            transform.position, rb.linearVelocity, moveSpeed, wallLayer, 
            wallDetectionRange);

        Vector2 totalAvoidance = (obstacleAvoidance + wallAvoidance) * avoidancePriority;
        
        Vector2 finalVelocity;
        if (totalAvoidance.sqrMagnitude > 0.01f)
        {
            float avoidanceStrength = totalAvoidance.magnitude;
            float desiredStrength = desired.magnitude;
            
            if (avoidanceStrength > desiredStrength * 0.3f)
            {
                finalVelocity = Vector2.Lerp(desired, totalAvoidance, 0.7f).normalized * moveSpeed;
            }
            else
            {
                finalVelocity = (desired + totalAvoidance).normalized * moveSpeed;
            }
        }
        else
        {
            finalVelocity = desired;
        }
        
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