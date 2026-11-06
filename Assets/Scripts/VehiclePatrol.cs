using UnityEngine;

public class VehiclePatrol : MonoBehaviour
{
    public enum RouteMode
    {
        Loop,       // last point → back to the first, round and round
        PingPong    // drive to the end, then back the way it came
    }


    // Inspector settings
    [Header("Route")]

    // The empty GameObject holding your patrol points as children.
    public Transform route;

    // Loop round, or go back and forth.
    public RouteMode mode = RouteMode.Loop;

    [Header("Driving")]

    // How fast it drives (metres per second).
    [Min(0.1f)] public float moveSpeed = 4f;

    // How fast it turns (degrees per second). Tanks turn slowly!
    [Min(1f)] public float turnSpeed = 60f;

    // If the next point is more than this many degrees off to the side,
    // the vehicle stops and turns on the spot first, like a real tank.
    [Range(1f, 180f)] public float turnInPlaceAngle = 25f;

    // How close it needs to get to a point to count as "arrived".
    [Min(0.05f)] public float arriveDistance = 0.5f;

    // How long it pauses at each point before moving on (seconds).
    [Min(0f)] public float waitTime = 1f;

    [Header("Ground")]

    // Keeps the vehicle sitting on the ground as it drives over dunes.
    public bool stickToGround = true;

    // Tilts the vehicle to match slopes, so it rides up and over hills.
    public bool alignToSlope = true;

    // Raise or lower the vehicle if it floats or sinks into the ground.
    public float groundOffset = 0f;

    // Which layers count as ground. "Everything" works for most scenes.
    public LayerMask groundLayers = ~0;

    int index;                          
    int direction = 1;                  
    float waitTimer;                    
    float heading;                      
    Vector3 groundNormal = Vector3.up;  


    // Start: runs once when the game begins.
    void Start()
    {
        // Start facing whichever way the vehicle is placed in the scene.
        heading = transform.eulerAngles.y;

        // Start by heading for whichever point is closest.
        index = NearestPoint();
    }



    void Update()
    {
        // No route set up? Do nothing.
        if (route == null || route.childCount == 0) return;

        // Pausing at a point? Count down and wait.
        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            StickToGround();
            return;
        }

        // Where we're heading, ignoring height (flat on the ground).
        Vector3 target = route.GetChild(index).position;
        Vector3 toTarget = target - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        // Arrived? Pause, then pick the next point.
        if (distance <= arriveDistance)
        {
            waitTimer = waitTime;
            NextPoint();
            return;
        }

        // Turning
        float wantedHeading = Quaternion.LookRotation(toTarget).eulerAngles.y;
        heading = Mathf.MoveTowardsAngle(heading, wantedHeading, turnSpeed * Time.deltaTime);

        // Driving
        float angleLeft = Mathf.Abs(Mathf.DeltaAngle(heading, wantedHeading));
        if (angleLeft < turnInPlaceAngle)
        {
            Vector3 forward = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
            float step = Mathf.Min(moveSpeed * Time.deltaTime, distance);
            transform.position += forward * step;
        }

        StickToGround();
    }


    // StickToGround: sits the vehicle on the ground and tilts it to slopes.

    void StickToGround()
    {
        if (stickToGround)
        {
            // Fire a laser straight down from above the vehicle.
            Vector3 origin = transform.position + Vector3.up * 5f;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 50f, groundLayers, QueryTriggerInteraction.Ignore);

            // Find the nearest thing it hit that ISN'T part of the vehicle itself.
            float nearest = float.MaxValue;
            bool found = false;
            RaycastHit groundHit = default;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;   // skip ourselves
                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    groundHit = hit;
                    found = true;
                }
            }

            // Found the ground? Sit on it.
            if (found)
            {
                Vector3 pos = transform.position;
                pos.y = groundHit.point.y + groundOffset;
                transform.position = pos;
                groundNormal = groundHit.normal;
            }
        }

        // Face the current heading...
        Quaternion facing = Quaternion.Euler(0f, heading, 0f);

        // ...and tilt to match the slope underneath, if ticked.
        if (alignToSlope && stickToGround)
        {
            Quaternion tilt = Quaternion.FromToRotation(Vector3.up, groundNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, tilt * facing, 8f * Time.deltaTime);
        }
        else
        {
            transform.rotation = facing;
        }
    }


    // NextPoint: picks which point to drive to next.
    void NextPoint()
    {
        int count = route.childCount;
        if (count < 2) return;

        if (mode == RouteMode.Loop)
        {
            // Go to the next point, wrapping round to the first at the end.
            index = (index + 1) % count;
        }
        else
        {
            // Turn round at either end of the route.
            if (index + direction >= count || index + direction < 0)
                direction = -direction;
            index += direction;
        }
    }


    // NearestPoint: finds the patrol point closest to the vehicle.  
    int NearestPoint()
    {
        if (route == null || route.childCount == 0) return 0;

        int best = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < route.childCount; i++)
        {
            float d = (route.GetChild(i).position - transform.position).sqrMagnitude;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }
        return best;
    }

    
    // OnDrawGizmos: draws the route in the Scene view (not in the game). 
    void OnDrawGizmos()
    {
        if (route == null || route.childCount == 0) return;

        Gizmos.color = Color.yellow;
        int count = route.childCount;

        for (int i = 0; i < count; i++)
        {
            Vector3 p = route.GetChild(i).position;

            // A sphere at each point...
            Gizmos.DrawWireSphere(p, 0.6f);

            // ...and a line to the next one.
            if (i + 1 < count)
                Gizmos.DrawLine(p, route.GetChild(i + 1).position);
        }

        // In Loop mode, also join the last point back to the first.
        if (mode == RouteMode.Loop && count > 2)
            Gizmos.DrawLine(route.GetChild(count - 1).position, route.GetChild(0).position);
    }
}
