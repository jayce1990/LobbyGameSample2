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
/// һ������LocalPlayer����LocalLobby,���Ҹ�LocalLobby�ѽ�����Ϸ״̬,���ｫ����ʵ��������Ϸ����������κ�����.
/// �⽫��������Ϸ������,���ڱ���������Ԥ����ʵ��������,�Լ�Ҫ���غ���ʾ����������ǵ����á�
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
    // NetworkManagerԤ����������ø�NGO������Ϸ����������ʲ����߼�.
    // UntiyTransport��ͨ��������һ������Relay���·���.
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
        if (!m_hasConnectedViaNGO)//���δ�ɹ�ͨ��NGO����,ǿ���˳���������Ϸ.
        {
            LogHandlerSettings.Instance.SpawnErrorPopup("Failed to join the game: Hasn't successfully connected via NGO.");
            OnGameEnd();
        }
    }
    /// <summary>
    /// ���������󷵻ر��ش���(��Ϸ�����������˳���Ϸ������ʧ��)��
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
    /// ����Allocation��JoinAllocation�����ӵ�Relay�������ķ������˵�.
    /// ���DTLS(ΪUDP���ƺ͸Ľ���TLSЭ��)����,�����п��õİ�ȫ�������˵�,��ʹ�ð�ȫ������.
    /// ����,ֻ�����ӵ�����ȫ��Relay IP��.
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
