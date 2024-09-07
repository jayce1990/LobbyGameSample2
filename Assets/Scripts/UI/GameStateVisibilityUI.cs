using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateVisibilityUI : UIPanelBase
{
    [SerializeField]
    GameState ShowThisWhen;

    void GameStateChanged(GameState state)
    {
        if (!ShowThisWhen.HasFlag(state))
            Hide();
        else
            Show();
    }

    public override void Start()
    {
        base.Start();

        Manager.onGameStateChanged += GameStateChanged;
    }

    private void OnDestroy()
    {
        if (Manager == null)
            return;

        Manager.onGameStateChanged -= GameStateChanged;
    }
}
