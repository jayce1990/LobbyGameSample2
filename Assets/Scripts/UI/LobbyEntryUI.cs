using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class LobbyEntryUI : MonoBehaviour
{
    //[SerializeField]
    //ColorLobbyUI m_ColorLobbyUI;
    [SerializeField]
    TMP_Text lobbyNameText;
    [SerializeField]
    TMP_Text lobbyCountText;

    public UnityEvent<LocalLobby> onLobbyPressed;
    LocalLobby m_Lobby;

    public void OnLobbyClicked()
    {
        onLobbyPressed?.Invoke(m_Lobby);
    }

    public void SetLobby(LocalLobby lobby)
    {
        m_Lobby = lobby;
        //m_ColorLobbyUI.SetLobby(lobby);

        SetLobbyNameText();
        SetLobbyCountText();
        m_Lobby.LobbyName.onChanged += (_) => { SetLobbyNameText(); };
        m_Lobby.onUserJoined += (_) => { SetLobbyCountText(); };
        m_Lobby.onUserLeft += (_) => { SetLobbyCountText(); };
    }

    void SetLobbyNameText()
    {
        if (m_Lobby != null)
            lobbyNameText.SetText(m_Lobby.LobbyName.Value);
    }

    void SetLobbyCountText()
    {
        if (m_Lobby != null)
            lobbyCountText.SetText($"{m_Lobby.PlayerCount}/{m_Lobby.MaxPlayerCount.Value}");
    }
}
