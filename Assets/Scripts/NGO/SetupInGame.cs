using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// 一旦本地LocalPlayer进入LocalLobby,并且该LocalLobby已进入游戏状态,这里将加载实际运行游戏部分所需的任何内容.
/// 这将存在于游戏场景内,用于保留对生成预制体实例的引用,以及要隐藏和显示场景里对象们的引用。
/// </summary>
public class SetupInGame : MonoBehaviour
{
    [SerializeField]
    GameObject m_IngameRunnerPrefab = default;
    [SerializeField]
    GameObject[] m_disableWhileInGame = default;

    GameObject m_InGameRunnerObj;

    bool m_doesNeedCleanup = false;
    bool m_hasConnectedViaNGO = false;

    LocalLobby m_lobby;

    public void StartNetworkedGame(LocalLobby localLobby, LocalPlayer localPlayer)
    {
        m_doesNeedCleanup = true;
        SetMenuVisibility(false);
#pragma warning disable 4014
        CreateNetworkManager(localLobby, localPlayer);
#pragma warning restore 4014
    }

    void SetMenuVisibility(bool areVisible)
    {
        foreach (GameObject go in m_disableWhileInGame)
        {
            go.SetActive(areVisible);
        }
    }

    /// <summary>
    // NetworkManager预制体包含设置该NGO迷你游戏所需的所有资产和逻辑.
    // UntiyTransport需通常被设置一个来自Relay的新分配.
    /// </summary>
    async Task CreateNetworkManager(LocalLobby localLobby, LocalPlayer localPlayer)
    {
        m_lobby = localLobby;
        m_InGameRunnerObj = Instantiate(m_IngameRunnerPrefab);
        //InGameRunner inGameRunner =  m_InGameRunnerObj.GetComponentInChildren<InGameRunner>();
        //inGameRunner.Initialize(OnConnectionVerified, m_lobby.PlayerCount, OnGameBegin, OnGameEnd, localPlayer);
        if (localPlayer.IsHost.Value)
        {
            await SetRelayHostData();
            NetworkManager.Singleton.StartHost();
        }
        else
        {
            await AwaitRelayCode();
            await SetRelayClientData();
            NetworkManager.Singleton.StartClient();
        }
    }
    void OnConnectionVerified()
    {
        m_hasConnectedViaNGO = true;
    }
    void OnGameBegin()
    {
        if (!m_hasConnectedViaNGO)//玩家未成功通过NGO连接,强制退出该迷你游戏.
        {
            LogHandlerSettings.Instance.SpawnErrorPopup("Failed to join the game: Hasn't successfully connected via NGO.");
            OnGameEnd();
        }
    }
    /// <summary>
    /// 比赛结束后返回本地大厅(游戏结束、主动退出游戏、连接失败)。
    /// </summary>
    public void OnGameEnd()
    {
        if (m_doesNeedCleanup)
        {
            NetworkManager.Singleton.Shutdown(true);
            Destroy(m_InGameRunnerObj);
            SetMenuVisibility(true);
            m_lobby.RelayCode.Value = "";
            GameManager.Instance.EndGame();
            m_doesNeedCleanup = false;
        }
    }
    async Task SetRelayHostData()
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponentInChildren<UnityTransport>();

        var allocation = await Relay.Instance.CreateAllocationAsync(m_lobby.MaxPlayerCount.Value);
        var joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);
        GameManager.Instance.HostSetRelayCode(joinCode);

        bool isSecure = false;
        var endpoint = GetEndpointForAllocation(allocation.ServerEndpoints, allocation.RelayServer.IpV4, allocation.RelayServer.Port, out isSecure);
        transport.SetHostRelayData(AddressFromEndpoint(endpoint), endpoint.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, isSecure);
    }
    async Task AwaitRelayCode()
    {
        string relayCode = m_lobby.RelayCode.Value;
        m_lobby.RelayCode.onChanged += (code) => relayCode = code;
        while (string.IsNullOrEmpty(relayCode))
        {
            await Task.Delay(100);
        }
    }
    async Task SetRelayClientData()
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponentInChildren<UnityTransport>();

        var allocation = await Relay.Instance.JoinAllocationAsync(m_lobby.RelayCode.Value);
        bool isSecure = false;
        var endpoint = GetEndpointForAllocation(allocation.ServerEndpoints, allocation.RelayServer.IpV4, allocation.RelayServer.Port, out isSecure);
        transport.SetClientRelayData(AddressFromEndpoint(endpoint), endpoint.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, allocation.HostConnectionData, isSecure);
    }
    /// <summary>
    /// 决定Allocation或JoinAllocation的连接到Relay服务器的服务器端点.
    /// 如果DTLS(为UDP定制和改进的TLS协议)可用,并且有可用的安全服务器端点,就使用安全的连接.
    /// 否则,只需连接到不安全的Relay IP上.
    /// </summary>
    NetworkEndPoint GetEndpointForAllocation(List<RelayServerEndpoint> endpoints, string ip, int port, out bool isSecure)
    {
#if ENABLE_MANAGED_UNITYTLS
        foreach (var endpoint in endpoints)
        {
            if (endpoint.Network == RelayServerEndpoint.NetworkOptions.Udp && endpoint.Secure)
            {
                isSecure = true;
                return NetworkEndPoint.Parse(endpoint.Host, (ushort)endpoint.Port);
            }
        }
#endif
        isSecure = false;
        return NetworkEndPoint.Parse(ip, (ushort)port);
    }
    string AddressFromEndpoint(NetworkEndPoint endpoint)
    {
        return endpoint.Address.Split(':')[0];
    }
}
