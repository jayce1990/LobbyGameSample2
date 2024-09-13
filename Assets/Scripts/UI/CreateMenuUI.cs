
using UnityEngine.UI;

public class CreateMenuUI : UIPanelBase
{
    public Button m_CreateButton;
    public JoinCreateLobbyUI m_JoinCreateLobbyUI;
    string m_ServerName;
    string m_ServerPassword;
    bool m_IsServerPrivate;

    public override void Start()
    {
        base.Start();
        m_CreateButton.interactable = false;
        m_JoinCreateLobbyUI.m_OnTabChanged.AddListener(OnTabChanged);
    }

    void OnTabChanged(EnumJoinCreateTabs joinCreateTabs)
    {
        if (joinCreateTabs == EnumJoinCreateTabs.Create)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    public void OnCreateButtonClick()
    {
        Manager.CreateLobby(m_ServerName, m_IsServerPrivate, m_ServerPassword);
    }
    bool ValidateServerName(string serverName)
    {
        if (serverName == null)
            return false;
        var serverNameLength = serverName.Length;
        return serverNameLength is > 0 and <= 64;
    }
    bool ValidatePassword(string password)
    {
        if (password == null)
            return true;
        var passwordLength = password.Length;
        if (passwordLength < 1)
            return true;
        return passwordLength is >= 8 and <= 64;
    }
    public void SetServerName(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            serverName = null;
        m_ServerName = serverName;
        m_CreateButton.interactable = ValidateServerName(m_ServerName) & ValidatePassword(m_ServerPassword);
    }
    public void SetServerPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            password = null;
        m_ServerPassword = password;
        m_CreateButton.interactable = ValidateServerName(m_ServerName) & ValidatePassword(m_ServerPassword);
    }
    public void SetPrivate(bool priv)
    {
        m_IsServerPrivate = priv;
    }

    private void OnDestroy()
    {
        m_JoinCreateLobbyUI.m_OnTabChanged.RemoveListener(OnTabChanged);
    }
}
