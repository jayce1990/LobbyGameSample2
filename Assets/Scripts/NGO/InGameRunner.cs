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
    int m_expectedPlayerCount;//Host使用
    float m_timeout = 10;
    bool m_hasConnected = false;

    PlayerData m_localPlayerData;//它的ID不一定是OwnerClientId,因为所有客户端都会看到所有生成的对象,而不管所有权如何.

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
        void FinishInitialize() //游戏初始化,创建NetworkObject等.
        {
        }
        if (IsHost)
            FinishInitialize();

        m_localPlayerData = new PlayerData(m_localPlayerData.name, NetworkManager.Singleton.LocalClientId);
        
        VerifyConnection_ServerRpc(m_localPlayerData.id);
    }
    public override void OnNetworkDespawn()
    {
        EndGame();//如果意外的断开连接,那么这里作为一个备份,去确保游戏内对象被清理.
    }
    /// <summary>
    /// 去验证连接,触发一个服务器RPC,然后触发一个客户端RPC调用.在这之后,开始真正的Setup.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    void VerifyConnection_ServerRpc(ulong clientId)
    {
        VerifyConnection_ClientRpc(clientId);

        //尽管这里我们现在可以池化符号对象,但后续加入的客户端将会被Spawn调用淹没.
        //这将会导致诸如InGameRunner's Spawn调用失败的发生,故我们将等到所有玩家都加入时.
        //(除此之外,我们将会显示说明书,用于在符号对象可以被生成前可以休息一下)
    }
    [ClientRpc]
    void VerifyConnection_ClientRpc(ulong clientId)
    {
        if (clientId == m_localPlayerData.id)
            VerifyConnectionConfirm_ServerRpc(m_localPlayerData);
    }
    /// <summary>
    /// 确认连接后,(生成一个玩家光标),并确认是否所有玩家都完成了连接.
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
    //游戏将会开始于:所有玩家都连接成功后 或 超时m_timeout秒后.
    void BeginGame()
    {
        onGameBeginning?.Invoke();

        //TODO:播放开场动画，播放完成后...继续在本类的回调里,开始游戏逻辑.
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
    /// TODO调用该函数.
    /// 当服务端游戏检测到结束条件达成: 开始走游戏结束的通信流程.
    /// 例如:玩家的Input传输到服务器Rpc后,在该事件里计算该玩家的得分已达到满分.
    /// </summary>
    void CheckGameEnd()
    {
        bool isGameEnd = true;//TODO:游戏结束的检测逻辑.
        if (isGameEnd)
        {
            //通知客户端 播放结束动画、请求战报、清理Networked对象等;
            //然后当Host本地的结束动画播放完成后,再通知客户端(EndGame_ClientRpc)真正结束,并Host本地结束.
            WaitForEndingSequence_ClientRpc();
        }
    }
    /// <summary>
    /// 服务器决定何时游戏结束.一旦结束,它需要先通知客户端们去清理它们的Networked对象.
    /// 因为一旦断开连接了,那么将会阻止客户端们去清理它们的Networked对象(因为它们无法从已断开连接的服务器接收到Despawn事件).
    /// </summary>
    [ClientRpc]
    void WaitForEndingSequence_ClientRpc()
    {
        //TODO:RPC请求游戏战报数据
        //TODO:播放谢幕动画,播放完成后...在本类的回调里TryEndGame().        
        TryEndGame();//注:因为还没有谢幕动画,因此这里直接调用动画完成后的回调TryEndGame().
    }
    void TryEndGame()
    {
        if (IsHost)
            StartCoroutine(TryEndGame_ClientsFirst());
    }
    IEnumerator TryEndGame_ClientsFirst()
    {
        TryEndGame_ClientRpc();//先发送客户端们的结束Rpc.
        yield return null;
        EndGame();//隔一帧后,Host本地结束.
    }
    [ClientRpc]
    void TryEndGame_ClientRpc()
    {
        if (IsHost)
            return;
        EndGame();//客户端们本地结束.
    }
    void EndGame()
    {
        m_onGameEnd?.Invoke();
    }
}
