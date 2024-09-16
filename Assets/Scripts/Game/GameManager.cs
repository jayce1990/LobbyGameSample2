
using LobbyRelaySample;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
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
    [SerializeField]
    CountDown m_countDown;

    VivoxSetup m_VivoxSetup = new VivoxSetup();
    [SerializeField]
    List<VivoxPlayerHandler> m_vivoxPlayerHandlers;

    #region Setup
    async Task InitializeServices()
    {
        string serviceProfileName = "player";
#if UNITY_EDITOR
        serviceProfileName = $"{serviceProfileName}{LocalProfileTool.LocalProfileSuffix}";
#endif
        await UnityServiceAuthenticator.TrySignInAsync(serviceProfileName);
        AuthenticatePlayer();

    }
    void AuthenticatePlayer()
    {
        var localId = AuthenticationService.Instance.PlayerId;
        m_LocalPlayer.ID.Value = localId;
        m_LocalPlayer.DisplayName.Value = NameGenerator.GetName(localId);
    }
    void StartVivoxLogin()
    {
        m_VivoxSetup.Initialize(m_vivoxPlayerHandlers, (didSucceed) =>
        {
            if (!didSucceed)
            {
                Debug.LogError("Vivox login failed! Retrying in 5s...");
                StartCoroutine(RetryConnection(StartVivoxLogin, m_LocalLobby.LobbyID.Value));
            }
        });
    }
    async void Awake()
    {
        Application.wantsToQuit += OnWantsToQuit;
        m_LocalPlayer = new LocalPlayer("", 0, false, "LocalPlayer");
        m_LocalLobby = new LocalLobby();
        LobbyManager = new LobbyManager();

        await InitializeServices();
        AuthenticatePlayer();
        StartVivoxLogin();
    }
    #endregion

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

        //重置LocalLobby
        m_LocalLobby.ResetLobby();
        m_LocalLobby.RelayServer = null;

        m_VivoxSetup.LeaveLobbyChannel();
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
    public void SetLocalPlayerStatus(EnumPlayerStatus playerStatus)
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
            if (!didSuccess)//加入大厅语音频道失败
            {
                StartCoroutine(RetryConnection(StartVivoxJoin, m_LocalLobby.LobbyID.Value));//5秒后重连StartVivoxJoin
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
                Debug.Log("CountDown Cancelled.");
                m_countDown.CancelCountDown();
            }
            if (enumLobbyState == EnumLobbyState.CountDown)
            {
                Debug.Log("CountDown Beginning.");
                m_countDown.StartCountDown();
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

        //大厅中玩家状态改变时,会计算Ready状态的玩家数量.触发这里的切换大厅的倒计时状态.
        m_LocalLobby.onPlayerReadyChange = readCount =>
        {
            if (readCount == m_LocalLobby.PlayerCount)//全部准备了,应该是倒计时状态.
            {
                if (m_LocalLobby.LocalLobbyState.Value != EnumLobbyState.CountDown)
                    m_LocalLobby.LocalLobbyState.Value = EnumLobbyState.CountDown;
            }
            else //未全部准备,不应该是倒计时状态.
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
            LogHandlerSettings.Instance.SpawnErrorPopup($"无法创建大厅:({exception.ErrorCode}){exception.Message}");
        }
    }

    public async void JoinLobby(string lobbyID, string lobbyCode, string password = null)
    {
        try
        {
            var lobby = await LobbyManager.JoinLobbyAsync(lobbyID, lobbyCode, m_LocalPlayer, password);
            LobbyConverters.RemoteToLocal(lobby, LocalLobby);
            m_LocalPlayer.IsHost.ForceSet(false);//因为默认值是false,故设置false时,虽相同值,也强制其触发UIEvent.
            await BindLobby();
        }
        catch (LobbyServiceException exception)
        {
            SetGameState(GameState.JoinMenu);
            LogHandlerSettings.Instance.SpawnErrorPopup($"无法加入大厅:({exception.ErrorCode}){exception.Message}");
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

        //SetCurrentLobbies
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
            LogHandlerSettings.Instance.SpawnErrorPopup("空名字是不允许的.");
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
    public void HostSetRelayCode(string code)
    {
        m_LocalLobby.RelayCode.Value = code;
        SendLocalLobbyData();
    }

    public void FinishedCountDown()
    {
        m_LocalPlayer.PlayerStatus.Value = EnumPlayerStatus.InGame;
        m_LocalLobby.LocalLobbyState.Value = EnumLobbyState.InGame;
        //m_setupInGame.StartNetworkedGame(m_LocalLobby, m_LocalPlayer);
    }
    public void BeginGame()
    {
        if (m_LocalPlayer.IsHost.Value)
        {
            m_LocalLobby.LocalLobbyState.Value = EnumLobbyState.InGame;
            m_LocalLobby.Locked.Value = true;
            SendLocalLobbyData();
        }
    }
    public void EndGame()
    {
        if (m_LocalPlayer.IsHost.Value)
        {
            m_LocalLobby.LocalLobbyState.Value = EnumLobbyState.Lobby;
            m_LocalLobby.Locked.Value = false;
            SendLocalLobbyData();
        }
        SetLobbyView();
    }
    public void UIChangeMenuState(GameState state)
    {
        var isQuittingGame = LocalGameState == GameState.Lobby && m_LocalLobby.LocalLobbyState.Value == EnumLobbyState.InGame;

        if (isQuittingGame)
        {
            state = GameState.Lobby;//如果在游戏状态,确保先从Lobby状态停止.
            EndGame();
            //m_setupInGame?.OnGameEnd();
        }
        SetGameState(state);
    }

    #region Teardown
    void ForceLeaveAttempt()
    {
        if (!string.IsNullOrEmpty(m_LocalLobby?.LobbyID.Value))
        {
#pragma warning disable 4014
            LobbyManager.LeaveLobbyAsync();
#pragma warning restore 4014
            m_LocalLobby = null;
        }
    }
    private void OnDestroy()
    {
        ForceLeaveAttempt();
        LobbyManager.Dispose();

    }
    IEnumerator LeaveBeforeQuit()
    {
        ForceLeaveAttempt();
        yield return null;
        Application.Quit();
    }
    bool OnWantsToQuit()
    {
        bool canQuit = string.IsNullOrEmpty(m_LocalLobby?.LobbyID.Value);
        StartCoroutine(LeaveBeforeQuit());
        return canQuit;
    }
    #endregion
}