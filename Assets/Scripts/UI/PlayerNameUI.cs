using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

//ÏÔÊ¾Íæ¼ÒÃû×Ö
public class PlayerNameUI : UIPanelBase
{
    [SerializeField]
    TMP_Text m_TextField;

    public override async void Start()
    {
        base.Start();

        var localPlayer = await GameManager.Instance.AwaitLocalPlayerInitialization();
        localPlayer.DisplayName.onChanged += SetNameText;
    }

    void SetNameText(string text)
    {
        m_TextField.SetText(text);
    }
}
