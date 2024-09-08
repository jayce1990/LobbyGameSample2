using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartLobbyButtonUI : UIPanelBase
{
    public void ToJoinMenu()
    {
        Manager.UIChangeMenuState(GameState.JoinMenu);
    }
}
