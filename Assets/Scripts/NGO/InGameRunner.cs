using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class InGameRunner : NetworkBehaviour
{
    public Action onGameBeginning;
    Action m_onConnectionVerfied;
    Action m_onGameEnd;
    int m_expectedPlayerCount;//Hostʹ��
    float m_timeout = 10;
    bool m_hasConnected = false;

    PlayerData m_localPlayerData;//����ID��һ����OwnerClientId,��Ϊ���пͻ��˶��ῴ���������ɵĶ���,����������Ȩ���.

    static InGameRunner s_Instance;
    public static InGameRunner Instance
    {
        get
        {
            if (s_Instance == null)
                s_Instance = FindObjectOfType<InGameRunner>();
            return s_Instance;
        }
    }

    public void Initialize(Action onConnectionVerfied, int expectedPlayerCount, Action onGameBegin, Action onGameEnd, string displayName)
    {
        m_onConnectionVerfied = onConnectionVerfied;
        m_expectedPlayerCount = expectedPlayerCount;
        onGameBeginning = onGameBegin;
        m_onGameEnd = onGameEnd;
        m_localPlayerData = new PlayerData(displayName, 0);
    }

    public override void OnNetworkSpawn()
    {
        void FinishInitialize() //��Ϸ��ʼ��,����NetworkObject��.
        {
        }
        if (IsHost)
            FinishInitialize();

        m_localPlayerData = new PlayerData(m_localPlayerData.name, NetworkManager.Singleton.LocalClientId);
        
        VerifyConnection_ServerRpc(m_localPlayerData.id);
    }
    public override void OnNetworkDespawn()
    {
        EndGame();//�������ĶϿ�����,��ô������Ϊһ������,ȥȷ����Ϸ�ڶ�������.
    }
    /// <summary>
    /// ȥ��֤����,����һ��������RPC,Ȼ�󴥷�һ���ͻ���RPC����.����֮��,��ʼ������Setup.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    void VerifyConnection_ServerRpc(ulong clientId)
    {
        VerifyConnection_ClientRpc(clientId);

        //���������������ڿ��Գػ����Ŷ���,����������Ŀͻ��˽��ᱻSpawn������û.
        //�⽫�ᵼ������InGameRunner's Spawn����ʧ�ܵķ���,�����ǽ��ȵ�������Ҷ�����ʱ.
        //(����֮��,���ǽ�����ʾ˵����,�����ڷ��Ŷ�����Ա�����ǰ������Ϣһ��)
    }
    [ClientRpc]
    void VerifyConnection_ClientRpc(ulong clientId)
    {
        if (clientId == m_localPlayerData.id)
            VerifyConnectionConfirm_ServerRpc(m_localPlayerData);
    }
    /// <summary>
    /// ȷ�����Ӻ�,(����һ����ҹ��),��ȷ���Ƿ�������Ҷ����������.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    void VerifyConnectionConfirm_ServerRpc(PlayerData clientData)
    {
        //PlayerCursor playerCursor = new PlayerCursor(m_playerCursorPrefab);
        //playerCursor.NetworkObject.SpawnWithOwnership(clientData.id);
        //playerCursor.name += clientData.name;

        bool areAllPlayersConnected = NetworkManager.Singleton.ConnectedClients.Count >= m_expectedPlayerCount;
        VerifyConnectionConfirm_ClientRpc(clientData.id, areAllPlayersConnected);
    }
    [ClientRpc]
    void VerifyConnectionConfirm_ClientRpc(ulong clientId, bool canBeginGame)
    {
        if (clientId == m_localPlayerData.id)
        {
            m_onConnectionVerfied?.Invoke();
            m_hasConnected = true;
        }

        if (m_hasConnected & canBeginGame)
        {
            m_timeout = -1;
            BeginGame();
        }
    }
    //��Ϸ���Ὺʼ��:������Ҷ����ӳɹ��� �� ��ʱm_timeout���.
    void BeginGame()
    {
        onGameBeginning?.Invoke();

        //TODO:���ſ���������������ɺ�...�����ڱ���Ļص���,��ʼ��Ϸ�߼�.
    }

    private void Update()
    {
        if (m_timeout >= 0)
        {
            m_timeout -= Time.deltaTime;
            if (m_timeout < 0)
                BeginGame();
        }
    }

    /// <summary>
    /// TODO���øú���.
    /// ���������Ϸ��⵽�����������: ��ʼ����Ϸ������ͨ������.
    /// ����:��ҵ�Input���䵽������Rpc��,�ڸ��¼���������ҵĵ÷��Ѵﵽ����.
    /// </summary>
    void CheckGameEnd()
    {
        bool isGameEnd = true;//TODO:��Ϸ�����ļ���߼�.
        if (isGameEnd)
        {
            //֪ͨ�ͻ��� ���Ž�������������ս��������Networked�����;
            //Ȼ��Host���صĽ�������������ɺ�,��֪ͨ�ͻ���(EndGame_ClientRpc)��������,��Host���ؽ���.
            WaitForEndingSequence_ClientRpc();
        }
    }
    /// <summary>
    /// ������������ʱ��Ϸ����.һ������,����Ҫ��֪ͨ�ͻ�����ȥ�������ǵ�Networked����.
    /// ��Ϊһ���Ͽ�������,��ô������ֹ�ͻ�����ȥ�������ǵ�Networked����(��Ϊ�����޷����ѶϿ����ӵķ��������յ�Despawn�¼�).
    /// </summary>
    [ClientRpc]
    void WaitForEndingSequence_ClientRpc()
    {
        //TODO:RPC������Ϸս������
        //TODO:����лĻ����,������ɺ�...�ڱ���Ļص���TryEndGame().        
        TryEndGame();//ע:��Ϊ��û��лĻ����,�������ֱ�ӵ��ö�����ɺ�Ļص�TryEndGame().
    }
    void TryEndGame()
    {
        if (IsHost)
            StartCoroutine(TryEndGame_ClientsFirst());
    }
    IEnumerator TryEndGame_ClientsFirst()
    {
        TryEndGame_ClientRpc();//�ȷ��Ϳͻ����ǵĽ���Rpc.
        yield return null;
        EndGame();//��һ֡��,Host���ؽ���.
    }
    [ClientRpc]
    void TryEndGame_ClientRpc()
    {
        if (IsHost)
            return;
        EndGame();//�ͻ����Ǳ��ؽ���.
    }
    void EndGame()
    {
        m_onGameEnd?.Invoke();
    }
}
