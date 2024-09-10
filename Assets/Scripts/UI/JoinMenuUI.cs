using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoinMenuUI : UIPanelBase
{

    public void OnRefresh()
    {
        Manager.QueryLobbies();
    }
}
