using UnityEngine;

public enum TileHighlight { None, Reachable, Selected, Attackable }

/// The cube you see for each tile. Gridman spawns these and tells each one what type it is.
public class SquareTile3D : MonoBehaviour
{
    public Vector2Int gridPos;
    public TileType type { get; private set; }

    private Renderer tileRenderer;
    private TileHighlight highlight = TileHighlight.None;

    // base colours match the gizmo colours in Gridman
    private Color openColor = Color.green;
    private Color coverColor = new Color(0.2f, 0.4f, 0.9f);
    private Color blockedColor = new Color(0.9f, 0.2f, 0.2f);

    private Color reachableColor = Color.cyan;
    private Color selectedColor = Color.yellow;
    private Color attackableColor = new Color(1f, 0.5f, 0f);//you can shoot 

    public void Setup(Vector2Int newGridPos, TileType newType)
    {
        tileRenderer = GetComponent<Renderer>();
        gridPos = newGridPos;
        type = newType;

        ApplyColor();
    }

    public void SetHighlight(TileHighlight newHighlight)
    {
        highlight = newHighlight;
        ApplyColor();
    }

    void ApplyColor()
    {
        if (tileRenderer == null)
            return;

        Color baseColor = openColor;
        if (type == TileType.Cover) baseColor = coverColor;
        else if (type == TileType.Blocked) baseColor = blockedColor;

       
        if (highlight == TileHighlight.Reachable)
            tileRenderer.material.color = Color.Lerp(baseColor, reachableColor, 0.6f);
        else if (highlight == TileHighlight.Selected)
            tileRenderer.material.color = selectedColor;
        else if (highlight == TileHighlight.Attackable)
            tileRenderer.material.color = attackableColor;
        else
            tileRenderer.material.color = baseColor;
    }
}
