using UnityEngine;

/// Line from the selected soldier to whatever is under the mouse.
/// Tile: cyan if you can walk there, red if you can't.  BadMan: orange if you can shoot it, red if you can't.
[RequireComponent(typeof(LineRenderer))]
public class TileTargetingLine : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;

    [Header("Line Settings")]
    public float lineHeight = 0.2f;
    public float lineWidth = 0.06f;

    public Color reachableColor = Color.cyan;
    public Color attackColor = new Color(1f, 0.5f, 0f);
    public Color unreachableColor = Color.red;

    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        lineRenderer.enabled = false;
    }

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        Soldier soldier = Soldier.Selected;

        if (mainCamera == null || soldier == null || soldier.IsMoving ||
            GameManager.Instance.State != GameState.PlayerTurn)
        {
            lineRenderer.enabled = false;
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(
            Input.mousePosition
        );

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // hovering a BadMan, show if we can shoot it
            BadMan hoveredBadMan = hit.collider.GetComponentInParent<BadMan>();
            if (hoveredBadMan != null)
            {
                Color attackLineColor =
                    soldier.CanAttack(hoveredBadMan)
                    ? attackColor
                    : unreachableColor;

                DrawLine(soldier, hoveredBadMan.transform.position, attackLineColor);
                return;
            }

            // hovering a tile, show if we can walk there
            SquareTile3D hoveredTile =
                hit.collider.GetComponent<SquareTile3D>();

            if (hoveredTile != null)
            {
                Color moveLineColor =
                    soldier.CanReach(hoveredTile.gridPos)
                    ? reachableColor
                    : unreachableColor;

                DrawLine(soldier, hoveredTile.transform.position, moveLineColor);
                return;
            }
        }

        lineRenderer.enabled = false;
    }

    void DrawLine(Soldier soldier, Vector3 targetPosition, Color lineColor)
    {
        Vector3 startPosition = soldier.transform.position;

        // Position both points just above the tile surface
        float surfaceY =
            Gridman.Instance.transform.position.y +
            Gridman.Instance.tileHeight / 2f +
            lineHeight;

        startPosition.y = surfaceY;
        targetPosition.y = surfaceY;

        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, targetPosition);

        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        lineRenderer.enabled = true;
    }
}
