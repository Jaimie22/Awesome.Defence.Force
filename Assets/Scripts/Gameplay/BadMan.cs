using UnityEngine;

public class BadMan : Alien
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public int preferredDistance = 8;

    protected override void Awake()
    {
        base.Awake();
        maxHealth = 5; moveRange = 4;
        coverBonus = 2f;   // this is how much it wants to be in cover higher is better

        // NEW: BadMan likes to stay 8 tiles away, so it needs to be able to shoot that far,
        // otherwise it would never get a shot off
        attackRange = 9;
    }

    protected override Unit ChooseTarget()
    {
        Unit weakest = null;
        foreach (var playerUnit in GameManager.Instance.PlayerUnits)
            if (weakest == null || playerUnit.Health < weakest.Health) weakest = playerUnit; // it wants to go for near dead units this will be changed because at times just ganking a guy is unfun
        return weakest;
    }

    protected override float GetTilePriority(Vector2Int tile, Unit target)
    {
        float priority = -Mathf.Abs(Gridman.Distance(tile, target.GridPos) - preferredDistance);
        if (Gridman.Instance.IsCover(tile)) priority += coverBonus;
        return priority;
    }


}