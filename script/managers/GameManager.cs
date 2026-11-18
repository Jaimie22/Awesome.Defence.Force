using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { Setup, PlayerTurn, EnemyTurn, Victory, Defeat }// all the states of gamin 

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.Setup;
    public int TurnNumber { get; private set; }// does what it says on the tin but helps cus it can challange the player to complete it in the least number of turns 

    readonly List<Unit> playerUnits = new List<Unit>();// records the list of how many player units there is and how many aliens there are 
    readonly List<Alien> aliens = new List<Alien>();

    // read-only views so the alien AI can look at the lists without being able to change them
    public IReadOnlyList<Unit> PlayerUnits => playerUnits;
    public IReadOnlyList<Alien> Aliens => aliens;

    public float delayBetweenEnemies = 0.5f;

    void Awake() => Instance = this;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() => StartPlayerTurn();// the player should always start 

    // this is what it says in the tin tells if a unit is there or not for the aliens 
    public void RegisterUnit(Unit unit)
    {
        if (unit is Alien alien) aliens.Add(alien); else playerUnits.Add(unit);// this checks if it is an alien and if it isnt then it is a player unit 
    }

    public void UnregisterUnit(Unit unit)
    {
        if (unit is Alien alien) aliens.Remove(alien); else playerUnits.Remove(unit);
        CheckWinLose();// it needs to check if this is the last unit 
    }


    public void StartPlayerTurn()
    {
        TurnNumber++;
        State = GameState.PlayerTurn;
        foreach (var unit in playerUnits) unit.StartTurn();// everyone gets their action points back
    }


    public void EndPlayerTurn()
    {
        if (State != GameState.PlayerTurn) return;
        Gridman.Instance.ClearHighlights();// hide the move tiles while the aliens go
        StartCoroutine(RunEnemyTurn());
    }

    IEnumerator RunEnemyTurn()// the enemy turn thing this will have a list that keeps track of aliens so that when you kill them they arnt still recorded and then it will end there turn after they do there jaz
    {
        State = GameState.EnemyTurn;

        foreach (var alien in new List<Alien>(aliens)) // this is the checker 
        {
            if (alien == null || !alien.IsAlive) continue;
            yield return StartCoroutine(alien.TakeTurn());// take turn is just a place holder for what the turn code will be 
            yield return new WaitForSeconds(delayBetweenEnemies);// a sec between each unit
            if (State == GameState.Defeat) yield break;
        }

        StartPlayerTurn();
    }

    void CheckWinLose()
    {
        if (State == GameState.Setup) return;
        if (aliens.Count == 0) { State = GameState.Victory; }// i will be adding more win and lose condtions and they will do for the prototype 
        else if (playerUnits.Count == 0) { State = GameState.Defeat; }
    }
}
