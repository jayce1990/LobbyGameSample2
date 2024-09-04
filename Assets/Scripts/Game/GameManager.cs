
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using UnityEngine;

[Flags]
public enum GameState
{
    Menu = 1,
    Lobby = 2,
    JoinMenu = 4
}

/// <summary>
/// 设置并运行项目
/// 所有重要数据在这里更新
/// 主场景的GameManager拥有运行游戏需要的所有引用
/// </summary>
public class GameManager : MonoBehaviour
{
    LocalPlayer m_LocalPlayer;
    LocalLobby m_LocalLobby;
    public LocalLobby LocalLobby => m_LocalLobby;
    public LocalLobbyList LobbyList { get; private set; } = new LocalLobbyList();

    public GameState LocalGameState { get; private set; }
    public Action<GameState> onGameStateChanged;

    EnumLobbyColor m_lobbyColorFilter;    
    public LobbyManager LobbyManager { get; private set; }

    static GameManager m_Instance;
    public static GameManager Instance
    {
        get
        {
            if (m_Instance == null)
                m_Instance = FindObjectOfType<GameManager>();
            return m_Instance;
        }
    }

    //[SerializeField]
    //SetupInGame m_setupInGame;
    //[SerializeField]
    //Countdown m_countdown;

    //VivoxSetup m_VivoxSetup = new VivoxSetup();
    //[SerializeField]
    //List<VivoxUserHandler> m_vivoxUserHandlers;

    public void SetLobbyColorFilter(int color)
    {
        m_lobbyColorFilter = (EnumLobbyColor)color;
    }

    public async Task<LocalPlayer> AwaitLocalPlayerInitialization()
    {
        while (m_LocalPlayer == null)
            await Task.Delay(100);
        return m_LocalPlayer;
    }

    void LeaveLobby()
    {
        m_LocalPlayer.ResetState();
#pragma warning disable 4014
        LobbyManager.LeaveLobbyAsync();
#pragma warning restore 4014
        m_LocalLobby.ResetLobby();
        m_LocalLobby.RelayServer = null;
        //m_VivoxSetup.LeaveLobbyChannel();
        LobbyList.Clear();
    }
    void SetGameState(GameState state)
    {
        var isLeavingLobby = LocalGameState == GameState.Lobby && (state == GameState.Menu || state == GameState.JoinMenu);
        LocalGameState = state;

        Debug.Log($"Switching Game State to : {LocalGameState}");

        if (isLeavingLobby)
            LeaveLobby();

        onGameStateChanged.Invoke(LocalGameState);
    }
    public async void CreateLobby(string name, bool isPrivate, string password = null, int maxPlayers = 4)
    {
        var lobby = await LobbyManager.CreateLobbyAsync(name, maxPlayers, isPrivate, m_LocalPlayer, password);
        LobbyConverters.RemoteToLocal(lobby, m_LocalLobby);
        m_LocalPlayer.IsHost.Value = true;
        //m_LocalLobby.onUserReadyChange = OnPlayersRead;
        try
        {
            //await BindLobby();
        }
        catch (LobbyServiceException exception)
        {
            SetGameState(GameState.JoinMenu);
            LogHandlerSettings.Instance.SpawnErrorPopup($"无法加入大厅:({exception.ErrorCode}){exception.Message}");
        }


    }
}