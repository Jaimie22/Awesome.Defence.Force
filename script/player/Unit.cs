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

    public int Health { get; protected set; }
    public int CurrentActionPoints { get; protected set; }
    public Vector2Int GridPos { get; protected set; }
    public bool IsAlive => Health > 0;

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

    public virtual void Attack(Unit target) => target.TakeDamage(attackDamage);

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