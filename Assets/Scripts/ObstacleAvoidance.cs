using UnityEngine;

public static class ObstacleAvoidance
{
    public static Vector2 GetAvoidanceForce(Vector2 position, Vector2 velocity, float moveSpeed, LayerMask obstacleLayer, float detectionRange = 2f, float raySpread = 30f, int rayCount = 5)
    {
        if (velocity.sqrMagnitude < 0.01f)
            return Vector2.zero;

        Vector2 forward = velocity.normalized;
        Vector2 avoidanceForce = Vector2.zero;
        int hitCount = 0;

        float halfSpread = raySpread * 0.5f;
        float step = rayCount > 1 ? raySpread / (rayCount - 1) : 0f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = -halfSpread + i * step;
            Vector2 rayDir = Quaternion.Euler(0, 0, angle) * forward;
            
            RaycastHit2D hit = Physics2D.Raycast(position, rayDir, detectionRange, obstacleLayer);
            
            if (hit.collider != null)
            {
                float distanceFactor = 1f - (hit.distance / detectionRange);
                Vector2 awayFromObstacle = (Vector2)hit.point - position;
                awayFromObstacle = Vector2.Perpendicular(awayFromObstacle).normalized;
                
                if (Vector2.Dot(awayFromObstacle, forward) < 0)
                    awayFromObstacle = -awayFromObstacle;
                
                avoidanceForce += awayFromObstacle * distanceFactor * distanceFactor;
                hitCount++;
            }
        }

        if (hitCount > 0)
        {
            avoidanceForce /= hitCount;
            avoidanceForce = avoidanceForce.normalized * moveSpeed;
        }

        return avoidanceForce;
    }

    public static Vector2 GetWallAvoidanceForce(Vector2 position, Vector2 velocity, float moveSpeed, LayerMask wallLayer, float detectionRange = 1.5f)
    {
        if (velocity.sqrMagnitude < 0.01f)
            return Vector2.zero;

        Vector2 forward = velocity.normalized;
        Vector2 avoidanceForce = Vector2.zero;
        int hitCount = 0;

        RaycastHit2D hitCenter = Physics2D.Raycast(position, forward, detectionRange, wallLayer);
        if (hitCenter.collider != null)
        {
            Vector2 awayFromWall = (Vector2)hitCenter.point - position;
            awayFromWall = Vector2.Perpendicular(awayFromWall).normalized;
            
            if (Vector2.Dot(awayFromWall, forward) < 0)
                awayFromWall = -awayFromWall;
            
            float distanceFactor = 1f - (hitCenter.distance / detectionRange);
            avoidanceForce += awayFromWall * distanceFactor * distanceFactor;
            hitCount++;
        }

        float sideAngle = 45f;
        Vector2 rayDirLeft = Quaternion.Euler(0, 0, sideAngle) * forward;
        Vector2 rayDirRight = Quaternion.Euler(0, 0, -sideAngle) * forward;

        RaycastHit2D hitLeft = Physics2D.Raycast(position, rayDirLeft, detectionRange, wallLayer);
        if (hitLeft.collider != null)
        {
            Vector2 awayFromWall = (Vector2)hitLeft.point - position;
            awayFromWall = Vector2.Perpendicular(awayFromWall).normalized;
            
            if (Vector2.Dot(awayFromWall, forward) < 0)
                awayFromWall = -awayFromWall;
            
            float distanceFactor = 1f - (hitLeft.distance / detectionRange);
            avoidanceForce += awayFromWall * distanceFactor * distanceFactor;
            hitCount++;
        }

        RaycastHit2D hitRight = Physics2D.Raycast(position, rayDirRight, detectionRange, wallLayer);
        if (hitRight.collider != null)
        {
            Vector2 awayFromWall = (Vector2)hitRight.point - position;
            awayFromWall = Vector2.Perpendicular(awayFromWall).normalized;
            
            if (Vector2.Dot(awayFromWall, forward) < 0)
                awayFromWall = -awayFromWall;
            
            float distanceFactor = 1f - (hitRight.distance / detectionRange);
            avoidanceForce += awayFromWall * distanceFactor * distanceFactor;
            hitCount++;
        }

        if (hitCount > 0)
        {
            avoidanceForce /= hitCount;
            avoidanceForce = avoidanceForce.normalized * moveSpeed;
        }

        return avoidanceForce;
    }
}