using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// Player unit. Click a soldier to select it, click a tile to move, click an alien to shoot it.
/// (movement and attack are the teammates GridPlayerMovement + PlayerAttack, now using Gridman and Unit)
public class Soldier : Unit
{
    public static Soldier Selected { get; private set; }

    [Header("Attack")]
    public int attackActionPointCost = 1;

    // NEW: like XCOM, firing uses up the rest of this soldier's turn. Untick to allow several shots.
    [Tooltip("If ticked, shooting uses up all of this soldier's remaining AP (like XCOM).")]
    public bool shootingEndsTurn = true;

    private Camera mainCamera;
    private bool isMoving = false;
    public bool IsMoving => isMoving;

    // every tile we can reach with the AP we have left, and the breadcrumbs to get there
    private Dictionary<Vector2Int, int> reachableTiles;
    private Dictionary<Vector2Int, Vector2Int> reachableParents;

    void Awake()
    {
        team = Team.Player;
        maxHealth = 10;
        attackRange = 5;
        attackDamage = 4;
    }

    void Start()
    {
        mainCamera = Camera.main;

        // soldier was placed in the scene by hand and never spawned, so snap it onto the nearest tile and give it its AP
        if (Health == 0) Spawn(Gridman.Instance.WorldToGrid(transform.position));

        // first soldier to show up is the selected one
        if (Selected == null) Select();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0) || isMoving)
            return;

        if (GameManager.Instance.State != GameState.PlayerTurn)
            return;

        // dont click through the end turn button
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
            return;

        // not the selected soldier, only thing we care about is being clicked on
        if (Selected != this)
        {
            bool selectedIsBusy = Selected != null && Selected.isMoving;
            if (!selectedIsBusy && hit.collider.GetComponentInParent<Soldier>() == this)
                Select();
            return;
        }

        Alien clickedAlien = hit.collider.GetComponentInParent<Alien>();
        if (clickedAlien != null)
        {
            TryAttack(clickedAlien);
            return;
        }

        SquareTile3D clickedTile = hit.collider.GetComponent<SquareTile3D>();
        if (clickedTile != null)
            TryMoveTo(clickedTile.gridPos);
    }

    public override void StartTurn()
    {
        base.StartTurn();
        if (Selected == this) RefreshHighlights();
    }

    void Select()
    {
        Selected = this;
        RefreshHighlights();
    }

    //  shmoving 
    void TryMoveTo(Vector2Int destination)
    {
        if (destination == GridPos)
            return;

        if (reachableTiles == null || !reachableTiles.ContainsKey(destination))
        {
            Debug.Log("Can't reach that tile!");// to check for testing
            return;
        }

        List<Vector2Int> path = Gridman.Instance.BuildPath(reachableParents, GridPos, destination);
        if (path.Count == 0)
            return;

        StartCoroutine(MoveAlongPath(path));
    }

    IEnumerator MoveAlongPath(List<Vector2Int> path)
    {
        isMoving = true;

        SpendActionPoints(path.Count);// 1 AP per tile

        Gridman.Instance.ClearHighlights();
        foreach (Vector2Int step in path)
            Gridman.Instance.SetHighlight(step, TileHighlight.Reachable);
        Gridman.Instance.SetHighlight(path[path.Count - 1], TileHighlight.Selected);

        yield return StartCoroutine(MoveAlong(path));

        isMoving = false;
        RefreshHighlights();
    }

    // used by the targeting line to colour itself
    public bool CanReach(Vector2Int tilePos) =>
        reachableTiles != null && reachableTiles.ContainsKey(tilePos);

    public void RefreshHighlights()
    {
        Gridman.Instance.ClearHighlights();

        reachableTiles = Gridman.Instance.GetReachable(GridPos, CurrentActionPoints, out reachableParents);
        foreach (Vector2Int tilePos in reachableTiles.Keys)
            Gridman.Instance.SetHighlight(tilePos, TileHighlight.Reachable);

        Gridman.Instance.SetHighlight(GridPos, TileHighlight.Selected);

        // orange tile under every Alien we could shoot right now
        foreach (Alien alien in GameManager.Instance.Aliens)
        {
            if (CanAttack(alien))
                Gridman.Instance.SetHighlight(alien.GridPos, TileHighlight.Attackable);
        }
    }

    //  Attacking 
    // in range AND enough AP left, the highlights and the targeting line both use this
    // NEW: also checks line of sight, so you can't shoot through walls
    public bool CanAttack(Alien target) =>
        CanShoot(target) && CanSpendActionPoints(attackActionPointCost);

    void TryAttack(Alien target)
    {
        if (!InAttackRange(target))
        {
            LastCombatMessage = "Out of range!";
            Debug.Log("Alien is out of attack range!");
            return;
        }

        // NEW: can't shoot through walls
        if (!HasLineOfSight(target))
        {
            LastCombatMessage = "No line of sight - something's in the way!";
            Debug.Log(LastCombatMessage);
            return;
        }

        if (!SpendActionPoints(attackActionPointCost))
        {
            LastCombatMessage = "Not enough AP!";
            Debug.Log("Not enough Action Points!");
            return;
        }

        // NEW: roll to hit, instead of always hitting
        Shoot(target);

        // NEW: firing ends this soldier's turn (if ticked)
        if (shootingEndsTurn)
            SpendActionPoints(CurrentActionPoints);

        RefreshHighlights();
    }
}