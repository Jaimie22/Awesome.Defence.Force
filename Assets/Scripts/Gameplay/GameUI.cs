using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text actionPointsText;
    public TMP_Text statusText;
    public Button endTurnButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(EndTurn);
    }
    // Update is called once per frame
    void Update()
    {
        Soldier soldier = Soldier.Selected;

        if (actionPointsText != null)
        {
            actionPointsText.text = soldier != null
                ? "AP: " + soldier.CurrentActionPoints + " / " + soldier.maxActionPoints
                : "AP: -";
        }

        if (statusText != null)
        {
            statusText.text =
                "Turn " + GameManager.Instance.TurnNumber +
                " - " + GameManager.Instance.State;
        }

        // space does the same as the button i want to make more hotkeys but we need a start 
        if (Input.GetKeyDown(KeyCode.Space))
            EndTurn();
    }

    void EndTurn()
    {
        // the turn can be ended mid walk but them must finish 
        if (Soldier.Selected != null && Soldier.Selected.IsMoving)
            return;

        GameManager.Instance.EndPlayerTurn();
    }
}
