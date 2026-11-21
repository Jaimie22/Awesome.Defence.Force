using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Team { Player, Alien }

/// Base for every unit (soldiers and aliens). Holds health, action points and movement.
public abstract class Unit : MonoBehaviour
{
    [Header("Stats")]
    public Team team;
    public int maxHealth = 10;
    public int moveRange = 5;
    public int attackRange = 6;
    public int attackDamage = 3;
    public float moveSpeed = 6f;

    [Header("Action Points")]
    public int maxActionPoints = 5;

    // NEW: everything that decides whether a shot hits
    [Header("Shooting")]
    [Tooltip("Base chance to hit (out of 100), before cover and distance are counted.")]
    [Range(0, 100)] public int aim = 75;

    [Tooltip("How much harder THIS unit is to hit while it's standing in cover.")]
    [Range(0, 100)] public int coverDefence = 35;

    [Tooltip("Targets this close or closer count as close range.")]
    [Min(0)] public int closeRange = 2;

    [Tooltip("Extra chance to hit at close range.")]
    [Min(0)] public int closeRangeBonus = 15;

    [Tooltip("Beyond this many tiles, shots start getting harder.")]
    [Min(0)] public int optimalRange = 4;

    [Tooltip("Chance to hit lost for every tile beyond the optimal range.")]
    [Min(0)] public int rangePenaltyPerTile = 5;

    [Tooltip("Shots can never be harder than this (so there's always a tiny chance).")]
    [Range(0, 100)] public int minHitChance = 5;

    // NEW: the last thing that happened in combat. GameUI shows this on screen.
    public static string LastCombatMessage { get; protected set; } = "";

    public int Health { get; protected set; }
    public int CurrentActionPoints { get; protected set; }
    public Vector2Int GridPos { get; protected set; }
    public bool IsAlive => Health > 0;

    // NEW: a tidy name for messages ("BadMan" instead of "BadMan(Clone)")
    public string DisplayName => name.Replace("(Clone)", "").Trim();

    private float halfHeight;

    public void Spawn(Vector2Int startTile)
    {
        Health = maxHealth;
        CurrentActionPoints = maxActionPoints;
        GridPos = startTile;
        halfHeight = GetHalfHeight();
        transform.position = Gridman.Instance.GetStandPosition(startTile, halfHeight);
        Gridman.Instance.Register(this);
        GameManager.Instance.RegisterUnit(this);
    }

    // works out how tall the unit is so it stands on the tile instead of sinking into it
    float GetHalfHeight()
    {
        Collider unitCollider = GetComponent<Collider>();
        if (unitCollider != null) return unitCollider.bounds.extents.y;

        Renderer unitRenderer = GetComponent<Renderer>();
        if (unitRenderer != null) return unitRenderer.bounds.extents.y;

        return 0.5f;
    }

    // Action points this will need to be reworked to lower ap to 2 and let all movement in range cost 1 and ablitys cost 1
    public virtual void StartTurn() => RestoreActionPoints();

    public bool CanSpendActionPoints(int amount) => amount >= 0 && CurrentActionPoints >= amount;

    public bool SpendActionPoints(int amount)
    {
        if (!CanSpendActionPoints(amount)) return false;

        CurrentActionPoints -= amount;
        Debug.Log(name + " AP remaining: " + CurrentActionPoints);
        return true;
    }

    public void RestoreActionPoints() => CurrentActionPoints = maxActionPoints;

    // Health this will be reworked
    public virtual void TakeDamage(int damage)
    {
        Health = Mathf.Max(Health - damage, 0);
        Debug.Log(name + " took " + damage + " damage! Health: " + Health);

        if (Health <= 0) Die();
    }

    protected virtual void Die()
    {
        Debug.Log(name + " has died!");
        Gridman.Instance.Unregister(this);
        GameManager.Instance.UnregisterUnit(this);
        Destroy(gameObject);
    }

    // Attacking  moving 
    public bool InAttackRange(Unit other) =>
        Gridman.Distance(GridPos, other.GridPos) <= attackRange;

    // the old guaranteed hit, kept in case anything still needs it
    public virtual void Attack(Unit target) => target.TakeDamage(attackDamage);

    // NEW: is there a wall or house between us and the target?
    // Fires an invisible line between the two units and checks if it hits anything on the Wall layer.
    public bool HasLineOfSight(Unit target)
    {
        if (target == null) return false;
        return !Physics.Linecast(transform.position, target.transform.position,
                                 Gridman.Instance.wallMask, QueryTriggerInteraction.Ignore);
    }

    // NEW: can we take a shot at this target right now? (alive, in range, and we can see it)
    public bool CanShoot(Unit target) =>
        target != null && target.IsAlive && InAttackRange(target) && HasLineOfSight(target);

    // NEW: works out the chance (out of 100) of hitting the target
    public int HitChance(Unit target)
    {
        int distance = Gridman.Distance(GridPos, target.GridPos);
        int chance = aim;

        // harder to hit someone in cover
        if (Gridman.Instance.IsCover(target.GridPos))
            chance -= target.coverDefence;

        // easier up close, harder far away
        if (distance <= closeRange)
            chance += closeRangeBonus;
        else if (distance > optimalRange)
            chance -= (distance - optimalRange) * rangePenaltyPerTile;

        return Mathf.Clamp(chance, minHitChance, 100);
    }

    // NEW: take a shot. Rolls a number from 0 to 99, and if it's under the hit chance, it hits.
    // Returns true if it hit.
    public virtual bool Shoot(Unit target)
    {
        int chance = HitChance(target);
        bool hit = Random.Range(0, 100) < chance;

        if (hit)
        {
            LastCombatMessage = DisplayName + " shoots " + target.DisplayName + " (" + chance + "%) - HIT for " + attackDamage + "!";
            Debug.Log(LastCombatMessage);
            target.TakeDamage(attackDamage);
        }
        else
        {
            LastCombatMessage = DisplayName + " shoots " + target.DisplayName + " (" + chance + "%) - MISSED!";
            Debug.Log(LastCombatMessage);
        }

        return hit;
    }

    public IEnumerator MoveAlong(List<Vector2Int> path)
    {
        foreach (Vector2Int step in path)
        {
            Vector3 targetPosition = Gridman.Instance.GetStandPosition(step, halfHeight);
            while ((transform.position - targetPosition).sqrMagnitude > 0.0001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                yield return null;
            }
            Gridman.Instance.MoveOccupant(this, GridPos, step);
            GridPos = step;
        }
    }
}