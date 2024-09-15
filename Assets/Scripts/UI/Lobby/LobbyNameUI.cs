using TMPro;
using UnityEngine;

public class LobbyNameUI : UIPanelBase
{
    [SerializeField]
    TMP_Text m_lobbyNameText;

    public override void Start()
    {
        base.Start();
        Manager.LocalLobby.LobbyName.onChanged += (s)=> { m_lobbyNameText.SetText(s); };
    }
}
