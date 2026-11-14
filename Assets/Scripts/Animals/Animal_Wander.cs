using System.Collections;
using UnityEngine;


public class AnimalWander : MonoBehaviour
{
    [Header("Parts")]

    // The Animator on the animal. Found automatically if left empty.
    public Animator animator;

    [Header("Animation State Names (must match the Animator boxes exactly)")]
    public string idleState = "Idle";
    public string walkState = "Walk";
    public string runState = "Run";
    public string eatStartState = "Eat";        // head going down to graze
    public string eatLoopState = "Eat Loop";    // grazing, on repeat
    public string sitDownState = "Sit Down";    // lying/sitting down
    public string sittingState = "Sitting";     // sitting, on repeat

    // How smoothly one animation blends into the next (seconds).
    [Min(0f)] public float blendTime = 0.25f;

    [Header("How Often Each Action Happens (higher = more often, 0 = never)")]
    [Min(0f)] public float idleChance = 3f;
    [Min(0f)] public float walkChance = 4f;
    [Min(0f)] public float runChance = 0.5f;
    [Min(0f)] public float eatChance = 3f;
    [Min(0f)] public float sitChance = 1f;

    [Header("How Long Actions Last (seconds: X = shortest, Y = longest)")]
    public Vector2 idleTime = new Vector2(2f, 5f);
    public Vector2 eatTime = new Vector2(4f, 10f);
    public Vector2 sitTime = new Vector2(6f, 15f);

    // How long the "Eat" and "Sit Down" animations take before their loops
    // start. Set these to roughly match each clip's length.
    [Min(0f)] public float eatStartLength = 1f;
    [Min(0f)] public float sitDownLength = 1f;

    [Header("Moving")]

    // Walking and running speeds (metres per second). Adjust until the
    // feet match the ground and don't look like they're sliding.
    [Min(0.1f)] public float walkSpeed = 1.2f;
    [Min(0.1f)] public float runSpeed = 4f;

    // How fast it turns (degrees per second).
    [Min(1f)] public float turnSpeed = 120f;

    // How far from its starting spot the animal will wander (metres).
    [Min(1f)] public float wanderRadius = 10f;

    // The shortest trip worth making (so it doesn't shuffle a few cm).
    [Min(0f)] public float minTravelDistance = 2f;

    // Gives up on a trip after this long, in case it gets stuck.
    [Min(1f)] public float maxTravelTime = 15f;

    [Header("Obstacles")]

    // How far ahead it looks for things in the way.
    [Min(0.1f)] public float lookAhead = 1.5f;

    // How high up it looks from (roughly chest height on the animal).
    [Min(0f)] public float eyeHeight = 0.8f;

    // Which layers count as obstacles. "Everything" works for most scenes.
    public LayerMask obstacleLayers = ~0;

    [Header("Ground")]

    // Keeps the animal's feet on the ground over bumps and slopes.
    public bool stickToGround = true;

    // Raise or lower it if it floats or sinks.
    public float groundOffset = 0f;

    // Which layers count as ground.
    public LayerMask groundLayers = ~0;


    Vector3 home;      
    float heading;      


    IEnumerator Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // We move the animal ourselves, so the animations mustn't move it too.
        if (animator != null) animator.applyRootMotion = false;

        home = transform.position;
        heading = transform.eulerAngles.y;

        // Small random pause at the start, so a herd doesn't all move in sync.
        Play(idleState);
        yield return new WaitForSeconds(Random.Range(0f, 2f));

        // The animal's "brain": pick an action, do it, repeat forever.
        while (true)
        {
            switch (PickAction())
            {
                case 0: yield return DoIdle(); break;
                case 1: yield return DoTravel(walkSpeed, walkState); break;
                case 2: yield return DoTravel(runSpeed, runState); break;
                case 3: yield return DoEat(); break;
                case 4: yield return DoSit(); break;
            }
        }
    }


    int PickAction()
    {
        float[] chances = { idleChance, walkChance, runChance, eatChance, sitChance };

        // Add them all up, then pick a random number within that total.
        float total = 0f;
        foreach (float c in chances) total += c;
        if (total <= 0f) return 0;   // everything set to 0? Just idle.

        float roll = Random.Range(0f, total);

        // Walk through the list until the roll lands on one.
        for (int i = 0; i < chances.Length; i++)
        {
            if (roll < chances[i]) return i;
            roll -= chances[i];
        }
        return 0;
    }


    // Stand around for a while.
    IEnumerator DoIdle()
    {
        Play(idleState);
        yield return new WaitForSeconds(Random.Range(idleTime.x, idleTime.y));
    }

    // Put its head down, then graze for a while.
    IEnumerator DoEat()
    {
        Play(eatStartState);
        yield return new WaitForSeconds(eatStartLength);

        Play(eatLoopState);
        yield return new WaitForSeconds(Random.Range(eatTime.x, eatTime.y));
    }

    // Sit down, then stay sitting for a while.
    IEnumerator DoSit()
    {
        Play(sitDownState);
        yield return new WaitForSeconds(sitDownLength);

        Play(sittingState);
        yield return new WaitForSeconds(Random.Range(sitTime.x, sitTime.y));
    }

    // Walk or run to a random spot within the wander area.
    IEnumerator DoTravel(float speed, string state)
    {
        // Find somewhere to go. If nowhere sensible, just idle instead.
        if (!PickDestination(out Vector3 target))
        {
            yield return DoIdle();
            yield break;
        }

        Play(state);
        float timer = 0f;

        while (timer < maxTravelTime)
        {
            timer += Time.deltaTime;

            // How far is left to go (flat, ignoring height)?
            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude < 0.5f) break;   // arrived!

            // Turn a little towards the target.
            float wanted = Quaternion.LookRotation(toTarget).eulerAngles.y;
            heading = Mathf.MoveTowardsAngle(heading, wanted, turnSpeed * Time.deltaTime);

            // Something in the way? Stop here and pick something else to do.
            if (BlockedAhead()) break;

            // Slow down while turning sharply, so it doesn't swing wide.
            float angleOff = Mathf.Abs(Mathf.DeltaAngle(heading, wanted));
            float slowDown = Mathf.Max(0.15f, Mathf.Cos(angleOff * Mathf.Deg2Rad));

            // Move forward the way it's facing.
            Vector3 forward = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
            transform.position += forward * speed * slowDown * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, heading, 0f);

            StickToGround();
            yield return null;   // wait for the next frame, then carry on
        }

        // Stop and stand still at the end of the trip.
        Play(idleState);
    }


    bool PickDestination(out Vector3 destination)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            // A random point in a circle around home.
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = home + new Vector3(offset.x, 0f, offset.y);

            // Too close to bother with? Try again.
            Vector3 flat = candidate - transform.position;
            flat.y = 0f;
            if (flat.magnitude < minTravelDistance) continue;

            // Is there something solid between here and there? Try again.
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            if (RaycastIgnoringSelf(eye, flat.normalized, flat.magnitude, obstacleLayers, out _)) continue;

            destination = candidate;
            return true;
        }

        destination = transform.position;
        return false;
    }


    bool BlockedAhead()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 forward = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
        return RaycastIgnoringSelf(eye, forward, lookAhead, obstacleLayers, out _);
    }


    void StickToGround()
    {
        if (!stickToGround) return;

        Vector3 origin = transform.position + Vector3.up * 3f;
        if (RaycastIgnoringSelf(origin, Vector3.down, 10f, groundLayers, out RaycastHit hit))
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y + groundOffset;
            transform.position = pos;
        }
    }

    bool RaycastIgnoringSelf(Vector3 origin, Vector3 direction, float distance, LayerMask mask, out RaycastHit result)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, mask, QueryTriggerInteraction.Ignore);

        float nearest = float.MaxValue;
        bool found = false;
        result = default;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;   // that's us, skip
            if (hit.distance < nearest)
            {
                nearest = hit.distance;
                result = hit;
                found = true;
            }
        }
        return found;
    }


    void Play(string state)
    {
        if (animator == null || string.IsNullOrEmpty(state)) return;

        // Warn (instead of failing silently) if the name doesn't match.
        if (!animator.HasState(0, Animator.StringToHash(state)))
        {
            Debug.LogWarning($"AnimalWander: no state called '{state}' in {name}'s Animator. Check the spelling.", this);
            return;
        }

        animator.CrossFadeInFixedTime(state, blendTime);
    }


    void OnDrawGizmosSelected()
    {
        Vector3 centre = Application.isPlaying ? home : transform.position;
        Gizmos.color = Color.green;

        // Draw the circle as lots of short lines.
        const int segments = 48;
        Vector3 previous = centre + new Vector3(wanderRadius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 next = centre + new Vector3(Mathf.Cos(angle) * wanderRadius, 0f, Mathf.Sin(angle) * wanderRadius);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
