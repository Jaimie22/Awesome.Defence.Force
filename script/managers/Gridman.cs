using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
    Open,      // normal land 
    Cover,     // cover
    Blocked    //  can't park here 
}

/// Builds a grid at start, labels each tile Open / Cover / Blocked by checking
/// which physics layers overlap it, and answers movement queries.
public class Gridman : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }
    public static Gridman Instance { get; private set; }

    [Header("Grid size ")]
    public int width = 20;
    public int height = 20;
    public float cellSize = 1f;// this is the odd man out it is how big the square is so that it fits the level well

    public LayerMask coverMask;     // objects on these layers make dis is cover
    public LayerMask blockedMask;   // can't park here 
    public float checkHeight = 2f;

    [Header("Tile visuals (the cubes you actually see, from the team mates grid)")]
    public GameObject tilePrefab;// the cube spawned for every tile, leave it empty if you only want the gizmos
    public float tileGap = 0.05f;// tiny gap between tiles so you can see the lines
    public float tileHeight = 0.15f;// how thick the tiles are, units stand on top of this

    TileType[,] tiles; // who is in a place so we don't have a pileup
    SquareTile3D[,] tileObjects;// the actual cubes in the scene, same layout as the tiles table
    readonly Dictionary<Vector2Int, Unit> occupants = new Dictionary<Vector2Int, Unit>();

    static readonly Vector2Int[] Dirs =
        { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };// this is the way that you can move all i have done right now is the cardinal directions to make it easy for me 

    void Awake()
    {
        Instance = this;
        BuildGrid();// check the level as it wakes up
    }


    public void BuildGrid() // how it's made m with the height and width that is added by us in the inspector 
    {
        ClearTileObjects();// if we rebuild dont leave the old cubes lying about
        tiles = new TileType[width, height];// the table 
        tileObjects = new SquareTile3D[width, height];
        Vector3 half = new Vector3(cellSize * 0.45f, checkHeight * 0.5f, cellSize * 0.45f);

        for (int tileX = 0; tileX < width; tileX++)// goes through each tile 
            for (int tileY = 0; tileY < height; tileY++)
            {
                Vector3 center = GridToWorld(new Vector2Int(tileX, tileY)) + Vector3.up * (checkHeight * 0.5f);

                if (Physics.CheckBox(center, half, Quaternion.identity, blockedMask))// we check the blocked first cus you can't take cover in a wall that kills you 
                    tiles[tileX, tileY] = TileType.Blocked;
                else if (Physics.CheckBox(center, half, Quaternion.identity, coverMask))
                    tiles[tileX, tileY] = TileType.Cover;// checks the cover
                else
                    tiles[tileX, tileY] = TileType.Open;// what you can walk in so we go most to least intrusive 
            }

        // second pass so the cubes we spawn never get picked up by the checks above
        if (tilePrefab == null) return;
        for (int tileX = 0; tileX < width; tileX++)
            for (int tileY = 0; tileY < height; tileY++)
                SpawnTileObject(new Vector2Int(tileX, tileY));
    }

    // makes the visible cube for one tile and tells it what type it is
    void SpawnTileObject(Vector2Int tilePos)
    {
        GameObject tileObject = Instantiate(tilePrefab, GridToWorld(tilePos), Quaternion.identity, transform);
        tileObject.name = "Tile_" + tilePos.x + "_" + tilePos.y;
        tileObject.transform.localScale = new Vector3(cellSize - tileGap, tileHeight, cellSize - tileGap);

        SquareTile3D tile = tileObject.GetComponent<SquareTile3D>();
        if (tile == null) tile = tileObject.AddComponent<SquareTile3D>();// so the prefab doesnt need the script on it

        tile.Setup(tilePos, tiles[tilePos.x, tilePos.y]);
        tileObjects[tilePos.x, tilePos.y] = tile;
    }

    void ClearTileObjects()
    {
        if (tileObjects == null) return;
        foreach (SquareTile3D tile in tileObjects)
            if (tile != null) Destroy(tile.gameObject);
    }


    public Vector3 GridToWorld(Vector2Int tilePos) =>
        transform.position + new Vector3(tilePos.x * cellSize, 0f, tilePos.y * cellSize);// where the grid is in the world 

    public Vector2Int WorldToGrid(Vector3 worldPos)// for checkinbg the tiles later 
    {
        Vector3 local = worldPos - transform.position;
        return new Vector2Int(Mathf.RoundToInt(local.x / cellSize), Mathf.RoundToInt(local.z / cellSize));
    }

    // where a unit should stand so its feet are on top of the tile and not buried in it
    public Vector3 GetStandPosition(Vector2Int tilePos, float unitHalfHeight) =>
        GridToWorld(tilePos) + Vector3.up * (tileHeight * 0.5f + unitHalfHeight);

    // tile colours (the cyan reachable ones and the yellow selected one)
    public void SetHighlight(Vector2Int tilePos, TileHighlight highlight)
    {
        if (!InBounds(tilePos) || tileObjects == null) return;
        SquareTile3D tile = tileObjects[tilePos.x, tilePos.y];
        if (tile != null) tile.SetHighlight(highlight);
    }

    public void ClearHighlights()
    {
        if (tileObjects == null) return;
        foreach (SquareTile3D tile in tileObjects)
            if (tile != null) tile.SetHighlight(TileHighlight.None);
    }

    // the tile questions 
    public bool InBounds(Vector2Int tilePos) => tilePos.x >= 0 && tilePos.y >= 0 && tilePos.x < width && tilePos.y < height;// in the map
    public TileType GetTile(Vector2Int tilePos) => InBounds(tilePos) ? tiles[tilePos.x, tilePos.y] : TileType.Blocked;//off the map is blocked i.e invisable wall
    public bool IsCover(Vector2Int tilePos) => GetTile(tilePos) == TileType.Cover;
    public bool IsUsable(Vector2Int tilePos) => InBounds(tilePos) && GetTile(tilePos) != TileType.Blocked;// tile that is useable 
    public bool IsOccupied(Vector2Int tilePos) => occupants.ContainsKey(tilePos);// is there a man there 

    /// Can a unit step onto this tile right now?
    public bool IsWalkable(Vector2Int tilePos) => IsUsable(tilePos) && !IsOccupied(tilePos);

    // this is to check if there is another unit that is in it 
    public void Register(Unit unit) => occupants[unit.GridPos] = unit;// the spawn in and put the name there 
    public void Unregister(Unit unit) { if (occupants.TryGetValue(unit.GridPos, out var occupant) && occupant == unit) occupants.Remove(unit.GridPos); }// you die you lose your place 
    public void MoveOccupant(Unit unit, Vector2Int from, Vector2Int to) { occupants.Remove(from); occupants[to] = unit; }// when you move take your name off the tile 

    public static int Distance(Vector2Int tileA, Vector2Int tileB) => Mathf.Abs(tileA.x - tileB.x) + Mathf.Abs(tileA.y - tileB.y); // this checks the move cost 


    /// BFS flood fill. Returns tile -> movement cost. 'parents' lets you rebuild paths.
    public Dictionary<Vector2Int, int> GetReachable(Vector2Int start, int range,
                                                    out Dictionary<Vector2Int, Vector2Int> parents)
    {
        var cost = new Dictionary<Vector2Int, int> { [start] = 0 }; // everty tile that you can get to
        parents = new Dictionary<Vector2Int, Vector2Int>(); // the path 
        var queue = new Queue<Vector2Int>();// the order to check 
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (cost[cur] >= range) continue;// the end of movement 

            foreach (var direction in Dirs)
            {
                var next = cur + direction;
                if (cost.ContainsKey(next) || !IsWalkable(next)) continue;
                cost[next] = cost[cur] + 1;
                parents[next] = cur;
                queue.Enqueue(next);
            }
        }
        return cost;
    }

    public List<Vector2Int> BuildPath(Dictionary<Vector2Int, Vector2Int> parents, Vector2Int start, Vector2Int end) // the path we move
    {
        var path = new List<Vector2Int>();
        var cur = end;
        while (cur != start)// how we follow the path
        {
            path.Add(cur);
            if (!parents.TryGetValue(cur, out cur)) return new List<Vector2Int>(); // unreachable
        }
        path.Reverse();// we then filp it i.e we know the path from the prious parts we have then moved form where we want to go back and basicly drew a line we flip the arrow and move 
        return path;
    }

    void OnDrawGizmos()// for us to see the grid 
    {
        for (int tileX = 0; tileX < width; tileX++)
            for (int tileY = 0; tileY < height; tileY++)
            {
                var tilePos = new Vector2Int(tileX, tileY);
                Vector3 tileCenter = GridToWorld(tilePos);
                Vector3 size = new Vector3(cellSize, 0.02f, cellSize);

                if (tiles != null)   // colour each tile 
                {
                    switch (tiles[tileX, tileY])
                    {
                        case TileType.Open: Gizmos.color = new Color(1, 1, 1, 0.25f); break;
                        case TileType.Cover: Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.6f); break;
                        case TileType.Blocked: Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f); break;
                    }
                    Gizmos.DrawCube(tileCenter, size * 0.95f);// grid gap
                }
                else
                {
                    Gizmos.color = new Color(1, 1, 1, 0.3f);
                    Gizmos.DrawWireCube(tileCenter, size);
                }
            }
    }
}
