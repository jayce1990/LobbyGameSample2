
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using UnityEngine;
using vivox;

[Flags]
public enum GameState
{
    Menu = 1,
    Lobby = 2,
    JoinMenu = 4
}

/// <summary>
/// ���ò�������Ŀ
/// ������Ҫ�������������
/// ��������GameManagerӵ��������Ϸ��Ҫ����������
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

    VivoxSetup m_VivoxSetup = new VivoxSetup();
    [SerializeField]
    List<VivoxPlayerHandler> m_vivoxPlayerHandlers;

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

    async void SendLocalPlayerData()
    {
        await LobbyManager.UpdatePlayerDataAsync(LobbyConverters.LocalToRemotePlayerData(m_LocalPlayer));
    }
    void SetLocalPlayerStatus(EnumPlayerStatus playerStatus)
    {
        m_LocalPlayer.PlayerStatus.Value = playerStatus;
        SendLocalPlayerData();
    }
    void SetLobbyView()
    {
        Debug.Log($"Setting Lobby user state {GameState.Lobby}");
        SetGameState(GameState.Lobby);
        SetLocalPlayerStatus(EnumPlayerStatus.Lobby);
    }
    IEnumerator RetryConnection(Action doConnection, string lobbyId)
    {
        yield return new WaitForSeconds(5);
        if (m_LocalLobby != null && m_LocalLobby.LobbyID.Value == lobbyId && !string.IsNullOrEmpty(lobbyId))
        {
            doConnection?.Invoke();
        }
    }
    void StartVivoxJoin()
    {
        m_VivoxSetup.JoinLobbyChannel(m_LocalLobby.LobbyID.Value, OnVivoxJoinComplete);
        void OnVivoxJoinComplete(bool didSuccess)
        {
            if (!didSuccess)//�����������Ƶ��ʧ��
            {
                StartCoroutine(RetryConnection(StartVivoxJoin, m_LocalLobby.LobbyID.Value));//5�������StartVivoxJoin
            }            
        }
    }
    async Task BindLobby()
    {
        await LobbyManager.BindLocalLobbyToRemote(m_LocalLobby.LobbyID.Value, m_LocalLobby);
        m_LocalLobby.LocalLobbyState.onChanged += enumLobbyState=> 
        {
            if (enumLobbyState == EnumLobbyState.Lobby)
            {
                Debug.Log("Countdown Cancelled.");
                //m_countdown.CancelCountDown();
            }
            if (enumLobbyState == EnumLobbyState.CountDown)
            {
                Debug.Log("Countdown Beginning.");
                //m_countdown.BeginCountDown();
            }
        };
        SetLobbyView();
        StartVivoxJoin();
    }
    public async void CreateLobby(string name, bool isPrivate, string password = null, int maxPlayers = 4)
    {
        var lobby = await LobbyManager.CreateLobbyAsync(name, maxPlayers, isPrivate, m_LocalPlayer, password);
        LobbyConverters.RemoteToLocal(lobby, m_LocalLobby);
        m_LocalPlayer.IsHost.Value = true;

        //���������״̬�ı�ʱ,�����Ready״̬���������.����������л������ĵ���ʱ״̬.
        m_LocalLobby.onPlayerReadyChange = readCount =>
        {
            if (readCount == m_LocalLobby.PlayerCount)//ȫ��׼����,Ӧ���ǵ���ʱ״̬.
            {
                if (m_LocalLobby.LocalLobbyState.Value != EnumLobbyState.CountDown)
                    m_LocalLobby.LocalLobbyState.Value = EnumLobbyState.CountDown;
            }
            else //δȫ��׼��,��Ӧ���ǵ���ʱ״̬.
            {
                if (m_LocalLobby.LocalLobbyState.Value == EnumLobbyState.CountDown)
                    m_LocalLobby.LocalLobbyState.Value = EnumLobbyState.Lobby;
            }
        };

        try
        {
            await BindLobby();
        }
        catch (LobbyServiceException exception)
        {
            SetGameState(GameState.JoinMenu);
            LogHandlerSettings.Instance.SpawnErrorPopup($"�޷���������:({exception.ErrorCode}){exception.Message}");
        }
    }

    public async void JoinLobby(string lobbyID, string lobbyCode, string password = null)
    {
        try
        {
            var lobby = await LobbyManager.JoinLobbyAsync(lobbyID, lobbyCode, m_LocalPlayer, password);
            LobbyConverters.RemoteToLocal(lobby, LocalLobby);
            m_LocalPlayer.IsHost.ForceSet(false);//��ΪĬ��ֵ��false,������falseʱ,����ֵͬ,Ҳǿ���䴥��UIEvent.
            await BindLobby();
        }
        catch (LobbyServiceException exception)
        {
            SetGameState(GameState.JoinMenu);
            LogHandlerSettings.Instance.SpawnErrorPopup($"�޷��������:({exception.ErrorCode}){exception.Message}");
        }        
    }

    public async void QuickJoin()
    {
        var lobby = await LobbyManager.QuickJoinLobbyAsync(m_LocalPlayer, m_lobbyColorFilter);
        if (lobby != null)
        {
            LobbyConverters.RemoteToLocal(lobby, m_LocalLobby);
            m_LocalPlayer.IsHost.ForceSet(false);
            await BindLobby();
        }
        else
        {
            SetGameState(GameState.JoinMenu);
        }
    }

    public async void QueryLobbies()
    {
        LobbyList.QueryState.Value = EnumLobbyQueryState.Fetching;
        var qr = await LobbyManager.RetrieveLobbyListAsync(m_lobbyColorFilter);
        if (qr == null)
        {
            return;
        }

        List<LocalLobby> lobbies = LobbyConverters.QueryToLocalList(qr);
        var newLobbyDict = new Dictionary<string, LocalLobby>();
        foreach (var lobby in lobbies)
            newLobbyDict.Add(lobby.LobbyID.Value, lobby);

        LobbyList.CurrentLobbies = newLobbyDict;
        LobbyList.QueryState.Value = EnumLobbyQueryState.Fetched;
    }


    public void SetLocalPlayerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            LogHandlerSettings.Instance.SpawnErrorPopup("�������ǲ������.");
            return;
        }

        m_LocalPlayer.DisplayName.Value = name;
        SendLocalPlayerData();
    }
    public void SetLocalPlayerEmote(EnumEmoteType emote)
    {
        m_LocalPlayer.Emote.Value = emote;
        SendLocalPlayerData();
    }
    async void SendLocalLobbyData()
    {
        await LobbyManager.UpdateLobbyDataAsync(LobbyConverters.LocalToRemoteLobbyData(m_LocalLobby));
    }
    public void SetLocalLobbyColor(int color)
    {
        if (m_LocalLobby.PlayerCount < 1)
            return;
        m_LocalLobby.LocalLobbyColor.Value = (EnumLobbyColor)color;
        SendLocalLobbyData();
    }

}