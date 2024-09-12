using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class JoinMenuUI : UIPanelBase
{
    [SerializeField] LobbyEntryUI m_LobbyEntryPrefab;
    [SerializeField] RectTransform m_LobbyButtonParent;
    [SerializeField] TMP_InputField m_JoinCodeField;

    public JoinCreateLobbyUI m_JoinCreateLobbyUI;

    Dictionary<string, LobbyEntryUI> m_LobbyButtons = new Dictionary<string, LobbyEntryUI>();

    LocalLobby m_LocalLobbySelected;
    string m_InputLobbyCode;
    string m_InputLobbyPwd;

    public override void Start()
    {
        base.Start();

        m_JoinCreateLobbyUI.m_OnTabChanged.AddListener(OnTabChanged);
        Manager.LobbyList.onLobbyListChange += OnLobbyListChanged;
    }

    public void OnRefreshClick()
    {
        Manager.QueryLobbies();
    }
    public void JoinMenuChangedVisibility(bool show)
    {
        if (show)
        {
            m_JoinCodeField.text = "";
            OnRefreshClick();
        }
    }
    public void OnLobbyCodeInputFieldChanged(string newCode)
    {
        if (!string.IsNullOrEmpty(newCode))
            m_InputLobbyCode = newCode.ToUpper();
    }
    public void OnLobbyPasswordInputFieldChanged(string newPwd)
    {
        if (!string.IsNullOrEmpty(newPwd))
            m_InputLobbyPwd = newPwd.ToUpper();
    }
    public void OnJoinClick()
    {
        if (m_LocalLobbySelected != null)
        {
            string selectedLobbyID = m_LocalLobbySelected.LobbyID.Value;
            Manager.JoinLobby(selectedLobbyID, m_InputLobbyCode, m_InputLobbyPwd);
            m_LocalLobbySelected = null;
        }
    }
    public void OnQuickJoinClick()
    {
        Manager.QuickJoin();
    }

    void OnTabChanged(EnumJoinCreateTabs joinCreateTabs)
    {
        if (joinCreateTabs == EnumJoinCreateTabs.Join)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }
    void RemoveLobbyButton(string lobbyID)
    {
        var lobbyButton = m_LobbyButtons[lobbyID];
        m_LobbyButtons.Remove(lobbyID);
        Destroy(lobbyButton.gameObject);
    }
    void OnLobbyListChanged(Dictionary<string, LocalLobby> localLobbys)
    {
        //删除已创建但没了的大厅,列表UI项.
        var removalList = new List<string>();
        foreach (var lobbyID in m_LobbyButtons.Keys)
        {
            if (!localLobbys.ContainsKey(lobbyID))
                removalList.Add(lobbyID);
        }
        foreach (var lobbyID in removalList)
            RemoveLobbyButton(lobbyID);

        //填充新的大厅数据到列表UI项,Create/Update/Del.
        foreach (var lobbyID in localLobbys.Keys)
        {
            var localLobby = localLobbys[lobbyID];
            bool canDisplay = localLobby.LocalLobbyState.Value == EnumLobbyState.Lobby && !localLobby.Private.Value;
            if (!m_LobbyButtons.ContainsKey(lobbyID))
            {
                if (canDisplay)//Create
                {
                    var lobbyButtonInstance = Instantiate(m_LobbyEntryPrefab, m_LobbyButtonParent);
                    lobbyButtonInstance.onLobbyPressed.AddListener(lobby => { m_LocalLobbySelected = lobby; });
                    m_LobbyButtons.Add(lobbyID, lobbyButtonInstance);
                    lobbyButtonInstance.SetLobby(localLobby);
                }
            }
            else
            {
                if (canDisplay)//Update
                {
                    m_LobbyButtons[lobbyID].SetLobby(localLobby);
                }
                else//Del
                {
                    RemoveLobbyButton(lobbyID);
                }
            }
        }
    }

    private void OnDestroy()
    {
        m_JoinCreateLobbyUI.m_OnTabChanged.RemoveListener(OnTabChanged);

        if (Manager == null)
            return;
        Manager.LobbyList.onLobbyListChange -= OnLobbyListChanged;
    }
}
