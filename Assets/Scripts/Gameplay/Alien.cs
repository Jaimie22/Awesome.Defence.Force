using System.Collections;
using UnityEngine;


public abstract class Alien : Unit
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    [Tooltip("Extra priority for standing on a cover tile. Keep small to just break ties, raise it to make aliens cover-hungry.")]
    public float coverBonus = 0.5f;

    // NEW: a short pause before and after an alien shoots, so the player can follow what's happening
    [Tooltip("Pause (seconds) before and after an alien shoots, so the player can follow the action.")]
    public float shotDelay = 0.4f;

    protected virtual void Awake() => team = Team.Alien;

    public IEnumerator TakeTurn()
    {
        StartTurn();

        Unit target = ChooseTarget();
        if (target == null) yield break;

        var reachable = Gridman.Instance.GetReachable(GridPos, moveRange, out var parents);

        Vector2Int bestTile = GridPos;
        float bestPriority = GetTilePriority(GridPos, target);

        foreach (var kvp in reachable)
        {
            float priority = GetTilePriority(kvp.Key, target);
            if (priority > bestPriority) { bestPriority = priority; bestTile = kvp.Key; }
        }

        if (bestTile != GridPos)
        {
            var path = Gridman.Instance.BuildPath(parents, GridPos, bestTile);
            yield return StartCoroutine(MoveAlong(path));
        }

        // NEW: after moving, shoot whoever we've got the best chance of hitting (if anyone's in sight)
        Unit shotTarget = ChooseShotTarget();
        if (shotTarget != null)
        {
            yield return new WaitForSeconds(shotDelay);
            Shoot(shotTarget);
            yield return new WaitForSeconds(shotDelay);
        }
    }


    /// it should move toward the player
    protected virtual Unit ChooseTarget()
    {
        Unit best = null;
        int bestDist = int.MaxValue;
        foreach (var playerUnit in GameManager.Instance.PlayerUnits)
        {
            int distance = Gridman.Distance(GridPos, playerUnit.GridPos);
            if (distance < bestDist) { bestDist = distance; best = playerUnit; }
        }
        return best;
    }

    // NEW: picks who to shoot: the soldier we can see, in range, with the best chance to hit
    protected virtual Unit ChooseShotTarget()
    {
        Unit best = null;
        int bestChance = -1;
        foreach (var playerUnit in GameManager.Instance.PlayerUnits)
        {
            if (!CanShoot(playerUnit)) continue;

            int chance = HitChance(playerUnit);
            if (chance > bestChance) { bestChance = chance; best = playerUnit; }
        }
        return best;
    }

    /// when it is moving if it can it should do so in cover
    protected virtual float GetTilePriority(Vector2Int tile, Unit target)
    {
        float priority = -Gridman.Distance(tile, target.GridPos);
        if (Gridman.Instance.IsCover(tile)) priority += coverBonus;
        return priority;
    }
}