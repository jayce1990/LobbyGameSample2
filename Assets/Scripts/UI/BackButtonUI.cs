using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackButtonUI : UIPanelBase
{
    public void ToJoinMenu()
    {
        Manager.UIChangeMenuState(GameState.JoinMenu);
    }

    public void ToMenu()
    {
        Manager.UIChangeMenuState(GameState.Menu);
    }
}
