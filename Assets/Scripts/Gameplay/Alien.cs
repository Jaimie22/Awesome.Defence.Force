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

    /// when it is moving if it can it should do so in cover
    protected virtual float GetTilePriority(Vector2Int tile, Unit target)
    {
        float priority = -Gridman.Distance(tile, target.GridPos);
        if (Gridman.Instance.IsCover(tile)) priority += coverBonus;
        return priority;
    }
}
