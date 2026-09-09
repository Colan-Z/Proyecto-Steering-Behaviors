using UnityEngine;

public class Guards : MonoBehaviour
{
    [Header("Configuración Escolta")]
    [SerializeField] private int escortIndex = 0;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float followDistance = 2.5f;
    [SerializeField] private float sideOffset = 1.8f;
    [SerializeField] private float arriveRadius = 0.3f;
    [SerializeField] private float slowDownRadius = 2f;

    [Header("Detección Obstáculos")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float detectionRange = 3f;
    [SerializeField] private float raySpread = 60f;
    [SerializeField] private int rayCount = 7;

    [Header("Evasión Jugador")]
    [SerializeField] private float playerAvoidRadius = 1.5f;
    [SerializeField] private float playerAvoidForce = 8f;

    [Header("Ajustes de Evasión")]
    [SerializeField] private float avoidanceStrength = 1.5f;
    [SerializeField] private float stuckBoostMultiplier = 2f;

    private Rigidbody2D rb;
    private Transform player;
    private PlayerController playerController;
    private Vector2 targetPosition;
    private Vector2 lastMoveInput;
    private Vector2 lastPosition;
    private float stuckTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!rb) { enabled = false; return; }

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }
    }

    void FixedUpdate()
    {
        if (!player || !playerController) return;

        Vector2 moveInput = playerController.MoveInput;
        if (moveInput.sqrMagnitude > 0.01f) lastMoveInput = moveInput.normalized;

        CalculateFormationPosition(moveInput);
        Vector2 velocity = CalculateSteering();
        rb.linearVelocity = velocity;
        
        CheckStuck();
    }

    void CheckStuck()
    {
        float moved = Vector2.Distance(rb.position, lastPosition);
        if (moved < 0.02f)
        {
            stuckTimer += Time.fixedDeltaTime;
        }
        else
        {
            stuckTimer = 0f;
        }
        lastPosition = rb.position;
    }

    void CalculateFormationPosition(Vector2 moveInput)
    {
        Vector2 dir = (moveInput.sqrMagnitude > 0.01f) ? moveInput.normalized : lastMoveInput;
        if (dir == Vector2.zero) return;

        bool horizontal = Mathf.Abs(dir.x) > Mathf.Abs(dir.y);
        Vector2 offset;

        if (horizontal)
        {
            offset = new Vector2(-dir.x * followDistance, (escortIndex == 0 ? 1 : -1) * sideOffset);
        }
        else
        {
            offset = new Vector2((escortIndex == 0 ? -1 : 1) * sideOffset * 0.7f, -dir.y * followDistance);
        }

        targetPosition = (Vector2)player.position + offset;
    }

    Vector2 CalculateSteering()
    {
        Vector2 pos = rb.position;
        Vector2 toTarget = targetPosition - pos;
        float dist = toTarget.magnitude;

        // Arrive behavior
        float speed = moveSpeed;
        if (dist < arriveRadius) speed = 0f;
        else if (dist < slowDownRadius) speed *= dist / slowDownRadius;

        Vector2 desired = (dist > 0.01f) ? toTarget.normalized * speed : Vector2.zero;

        // Obstacle avoidance
        Vector2 avoidance = GetPredictiveAvoidance(pos, desired);
        
        // Player avoidance
        Vector2 playerAvoid = GetPlayerAvoidance(pos);

        // Si se traba, aumentar fuerza de evasión
        if (stuckTimer > 0.5f && dist > 1f)
        {
            // Buscar la dirección de escape
            Vector2 escapeDir = FindEscapeDirection(pos, toTarget.normalized);
            if (escapeDir != Vector2.zero)
            {
                // Fuerza el movimiento en dirección de escape
                Vector2 forcedMove = escapeDir * moveSpeed * stuckBoostMultiplier;
                return forcedMove;
            }
        }

        Vector2 result = desired + avoidance + playerAvoid;
        if (result.sqrMagnitude > moveSpeed * moveSpeed)
            result = result.normalized * moveSpeed;

        return result;
    }

    Vector2 GetPredictiveAvoidance(Vector2 pos, Vector2 velocity)
    {
        if (velocity.sqrMagnitude < 0.01f) return Vector2.zero;

        Vector2 forward = velocity.normalized;
        Vector2 force = Vector2.zero;
        int hits = 0;

        float half = raySpread * 1f;
        float step = rayCount > 1 ? raySpread / (rayCount - 1) : 0f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = -half + i * step;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * forward;
            RaycastHit2D hit = Physics2D.Raycast(pos, dir, detectionRange, obstacleLayer);

            if (hit.collider)
            {
                float t = 1f - hit.distance / detectionRange;
                Vector2 perp = Vector2.Perpendicular(hit.point - pos).normalized;
                if (Vector2.Dot(perp, forward) < 0) perp = -perp;
                force += perp * t * t;
                hits++;
            }
        }

        if (hits > 0)
        {
            Vector2 avoidanceForce = (force / hits).normalized * moveSpeed * avoidanceStrength;
            
            // Si hay muchos impactos aumenta la fuerza
            if (hits >= 3)
            {
                avoidanceForce *= 1.5f;
            }
            
            return avoidanceForce;
        }

        return Vector2.zero;
    }

    Vector2 FindEscapeDirection(Vector2 pos, Vector2 targetDir)
    {
        Vector2 bestDir = Vector2.zero;
        float bestScore = -1f;
        
        // Prueba 16 direcciones
        for (int i = 0; i < 16; i++)
        {
            float angle = (i / 16f) * 360f;
            Vector2 testDir = Quaternion.Euler(0, 0, angle) * targetDir;
            
            // Verificar si la dirección está libre
            RaycastHit2D hit = Physics2D.Raycast(pos, testDir, detectionRange, obstacleLayer);
            float distance = hit.collider ? hit.distance : detectionRange;
            
            // Puntuación: priorizar dirección libre y cercana al objetivo
            float angleToTarget = Vector2.Angle(testDir, targetDir);
            float score = distance * (1f - (angleToTarget / 180f) * 0.5f);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestDir = testDir;
            }
        }
        
        return bestDir.normalized;
    }

    Vector2 GetPlayerAvoidance(Vector2 pos)
    {
        Vector2 toPlayer = (Vector2)player.position - pos;
        float dist = toPlayer.magnitude;
        
        if (dist < playerAvoidRadius && dist > 0.01f)
        {
            return (-toPlayer.normalized) * playerAvoidForce * (1f - dist / playerAvoidRadius);
        }
        return Vector2.zero;
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !player) return;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, targetPosition);
        Gizmos.DrawWireSphere(targetPosition, 0.25f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPosition, slowDownRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(targetPosition, arriveRadius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, playerAvoidRadius);
        
        if (stuckTimer > 0.5f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
}