using UnityEngine;

public class Spawner : MonoBehaviour
{
    public Unit soldierPrefab;
    public Unit badPrefab;

    public Vector2Int soldierStart = new Vector2Int(2, 2);
    public Vector2Int gruntStart = new Vector2Int(15, 15);
    public Vector2Int sniperStart = new Vector2Int(17, 12);

    void Start()
    {
        Spawn(soldierPrefab, soldierStart);
        Spawn(badPrefab, gruntStart);

    }

    void Spawn(Unit prefab, Vector2Int startTile)
    {
        Unit unit = Instantiate(prefab);
        unit.Spawn(startTile);
    }
}

