using System.Collections.Generic;
using UnityEngine;

public class LobbyPlayerListUI : UIPanelBase
{
    [SerializeField]
    List<InLobbyPlayerUI> m_PlayerUIObjects = new List<InLobbyPlayerUI>();

    LocalLobby m_LocalLobby;

    public override void Start()
    {
        base.Start();

        m_LocalLobby = GameManager.Instance.LocalLobby;

        m_LocalLobby.onPlayerJoined += (_)=> { SynchPlayerUI(); };
        m_LocalLobby.onPlayerLeft += (_)=> { SynchPlayerUI(); };
    }

    void SynchPlayerUI()
    {
        foreach (var ui in m_PlayerUIObjects)
            ui.ResetUI();

        for (int i = 0; i < m_LocalLobby.PlayerCount; i++)
        {
            var playerUIObject = m_PlayerUIObjects[i];
            var player = m_LocalLobby.GetLocalPlayer(i);
            if (player == null)
                continue;
            playerUIObject.SetPlayer(player);
        }
    }
}
