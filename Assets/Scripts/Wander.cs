using UnityEngine;

public class Wander : MonoBehaviour
{
    [Header("Configuración Wander")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float changeDirectionInterval = 2f;
    [SerializeField] private float wanderRadius = 5f;

    private Rigidbody2D rb;
    private Vector2 currentDirection;
    private float timer;
    private Vector2 startPosition;

void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError($"[Wander] NO hay Rigidbody2D en: {gameObject.name}");
            enabled = false;
            return;
        }
        startPosition = transform.position;
        PickNewDirection();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= changeDirectionInterval)
        {
            PickNewDirection();
            timer = 0f;
        }

        rb.linearVelocity = currentDirection * moveSpeed;
    }

    private void PickNewDirection()
    {
        Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
        Vector2 targetPos = startPosition + randomOffset;
        currentDirection = (targetPos - (Vector2)transform.position).normalized;

        if (currentDirection == Vector2.zero)
        {
            currentDirection = Random.insideUnitCircle.normalized;
        }
    }

private void OnCollisionEnter2D(Collision2D collision)
    {
        PickNewDirection();
    }
}