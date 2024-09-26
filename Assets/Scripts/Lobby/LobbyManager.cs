using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 对LobbyAPI的直接调用和您直接想要结果的抽象层.
/// 例如,可以请求获取一个可读的列表,而不需要直接的查询调用.
/// </summary>
/// 
/// 同一时间只管理一个大厅,只有通过JoinAsync,CreateAsync,和QuickJoinAsync才能进入带有ID的大厅.

public class LobbyManager : IDisposable
{
    const string key_RelayCode = nameof(LocalLobby.RelayCode);
    const string key_LobbyState = nameof(LocalLobby.LocalLobbyState);
    const string key_LobbyColor = nameof(LocalLobby.LocalLobbyColor);

    const string key_Displayname = nameof(LocalPlayer.DisplayName);
    const string key_Emote = nameof(LocalPlayer.Emote);
    const string key_PlayerStatus = nameof(LocalPlayer.PlayerStatus);

    public Lobby CurrentLobby => m_CurrentLobby;
    Lobby m_CurrentLobby;
    LobbyEventCallbacks m_LobbyEventCallbacks = new LobbyEventCallbacks();
    const int k_maxLobbiesToShow = 16;//如果要更多,考虑获取翻页的结果或者使用过滤

    Task m_HeartBeatTask;

    //根据UI[RateLimitVisibility组件]选择的EnumRequestType所对应ServiceRateLimiter的冷却状态改变事件,设置UI节点的显隐.
    #region Rate Limiting
    public enum EnumRequestType
    {
        Query = 0,
        Join,
        QuickJoin,
        Host
    }
    public ServiceRateLimiter GetRateLimiter(EnumRequestType type)
    {
        if (type == EnumRequestType.Join)
            return m_JoinCooldown;
        if (type == EnumRequestType.QuickJoin)
            return m_QuickJoinCooldown;
        if (type == EnumRequestType.Host)
            return m_CreateCooldown;

        return m_QueryCooldown;
    }
    //RateLimits限制在这: https://docs.unity.com/lobby/rate-limits.html
    ServiceRateLimiter m_QueryCooldown = new ServiceRateLimiter(1, 1f);
    ServiceRateLimiter m_CreateCooldown = new ServiceRateLimiter(2, 6f);
    ServiceRateLimiter m_JoinCooldown = new ServiceRateLimiter(2, 6f);
    ServiceRateLimiter m_QuickJoinCooldown = new ServiceRateLimiter(1, 10f);
    ServiceRateLimiter m_GetLobbyCooldown = new ServiceRateLimiter(1, 1f);
    ServiceRateLimiter m_DeleteLobbyCooldown = new ServiceRateLimiter(2, 1f);
    ServiceRateLimiter m_UpdateLobbyCooldown = new ServiceRateLimiter(5, 5f);
    ServiceRateLimiter m_UpdatePlayerCooldown = new ServiceRateLimiter(5, 5f);
    ServiceRateLimiter m_LeaveLobbyOrRemovePlayer = new ServiceRateLimiter(5, 1f);
    ServiceRateLimiter m_HeartBeatCooldown = new ServiceRateLimiter(5, 30f);
    #endregion

    public bool InLobby()
    {
        if (m_CurrentLobby == null)
        {
            Debug.LogWarning("LobbyManager当前不在一个大厅中,是否CreateLobbyAsync or JoinLobbyAsync?");
            return false;
        }
        return true;
    }

    Dictionary<string, PlayerDataObject> CreateInitialPlayerData(LocalPlayer user)
    {
        Dictionary<string, PlayerDataObject> data = new Dictionary<string, PlayerDataObject>();

        var displayNameObject = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, user.DisplayName.Value);
        data.Add("DisplayName", displayNameObject);
        return data;
    }

    //把远程大厅、玩家事件和数据改变事件,绑定到本地的大厅、玩家的数据改变.
    public async Task BindLocalLobbyToRemote(string lobbyID, LocalLobby localLobby)
    {
        m_LobbyEventCallbacks.LobbyChanged += changes =>
        {
            //Lobby Fields
            if (changes.Name.Changed)
                localLobby.LobbyName.Value = changes.Name.Value;
            if (changes.HostId.Changed)
                localLobby.HostID.Value = changes.HostId.Value;
            if (changes.IsPrivate.Changed)
                localLobby.Private.Value = changes.IsPrivate.Value;
            if (changes.IsLocked.Changed)
                localLobby.Locked.Value = changes.IsLocked.Value;
            if (changes.AvailableSlots.Changed)
                localLobby.AvailableSlots.Value = changes.AvailableSlots.Value;
            if (changes.MaxPlayers.Changed)
                localLobby.MaxPlayerCount.Value = changes.MaxPlayers.Value;
            if (changes.LastUpdated.Changed)
                localLobby.LastUpdated.Value = changes.LastUpdated.Value.ToFileTimeUtc();

            //Custom Lobby Fields
            if (changes.PlayerData.Changed)
            {
                foreach (var lobbyPlayerChanges in changes.PlayerData.Value)
                {
                    var playerIndex = lobbyPlayerChanges.Key;
                    var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                    if (localLobby == null)
                        continue;
                    var playerChanges = lobbyPlayerChanges.Value;
                    if (playerChanges.ConnectionInfoChanged.Changed)
                    {
                        var connectionInfo = playerChanges.ConnectionInfoChanged.Value;
                        Debug.Log($"ConnectionInfo for player {playerIndex} changed to {connectionInfo}.");
                    }

                    if (playerChanges.LastUpdatedChanged.Changed)
                    {
                    }
                }
            }

        };
        m_LobbyEventCallbacks.LobbyDeleted += async () => { await LeaveLobbyAsync(); };

        void DataAddedOrChanged(Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>> changes)
        {
            foreach (var change in changes)
            {
                var changedValue = change.Value;
                var changedKey = change.Key;

                if (changedKey == key_RelayCode)
                    localLobby.RelayCode.Value = changedValue.Value.Value;
                if (changedKey == key_LobbyState)
                    localLobby.LocalLobbyState.Value = (EnumLobbyState)int.Parse(changedValue.Value.Value);
                if (changedKey == key_LobbyColor)
                    localLobby.LocalLobbyColor.Value = (EnumLobbyColor)int.Parse(changedValue.Value.Value);
            }
        }
        m_LobbyEventCallbacks.DataAdded += changes => { DataAddedOrChanged(changes); };
        m_LobbyEventCallbacks.DataChanged += changes => { DataAddedOrChanged(changes); };
        m_LobbyEventCallbacks.DataRemoved += changes =>
        {
            foreach (var change in changes)
            {
                if (change.Key == key_RelayCode)
                    localLobby.RelayCode.Value = "";
            }
        };

        m_LobbyEventCallbacks.KickedFromLobby += () => { Debug.Log("Left Lobby"); Dispose(); };
        m_LobbyEventCallbacks.LobbyEventConnectionStateChanged += lobbyEventConnectionState => { Debug.Log($"大厅连接状态变成{lobbyEventConnectionState}."); };

        void ParseCustomPlayerData(LocalPlayer localPlayer, string dataKey, string dataValue)
        {
            if (dataKey == key_Displayname)
                localPlayer.DisplayName.Value = dataValue;
            if (dataKey == key_Emote)
                localPlayer.Emote.Value = (EnumEmoteType)int.Parse(dataValue);
            if (dataKey == key_PlayerStatus)
                localPlayer.PlayerStatus.Value = (EnumPlayerStatus)int.Parse(dataValue);

        }
        m_LobbyEventCallbacks.PlayerJoined += players =>
        {
            foreach (var playerChanges in players)
            {
                Player player = playerChanges.Player;

                var id = player.Id;
                var index = playerChanges.PlayerIndex;
                var isHost = localLobby.HostID.Value == id;

                var newPlayer = new LocalPlayer(id, index, isHost);
                foreach (var dataKey in player.Data.Keys)
                {
                    ParseCustomPlayerData(newPlayer, dataKey, player.Data[dataKey].Value);
                }
                localLobby.AddPlayer(index, newPlayer);
            }
        };
        m_LobbyEventCallbacks.PlayerLeft += players =>
        {
            foreach (var player in players)
            {
                localLobby.RemovePlayer(player);
            }
        };
        void PlayerDataAddOrChanged(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> changes)
        {
            foreach (var lobbyPlayerChanges in changes)
            {
                var playerIndex = lobbyPlayerChanges.Key;
                var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                if (localPlayer == null)
                    continue;
                var playerChanges = lobbyPlayerChanges.Value;

                foreach (var playerChange in playerChanges)
                {
                    var changedValue = playerChange.Value;
                    var playerDataObject = changedValue.Value;
                    ParseCustomPlayerData(localPlayer, playerChange.Key, playerDataObject.Value);
                }
            }
        }
        m_LobbyEventCallbacks.PlayerDataAdded += changes => { PlayerDataAddOrChanged(changes); };
        m_LobbyEventCallbacks.PlayerDataChanged += changes => { PlayerDataAddOrChanged(changes); };
        m_LobbyEventCallbacks.PlayerDataRemoved += changes =>
        {
            foreach (var lobbyPlayerChanges in changes)
            {
                var playerIndex = lobbyPlayerChanges.Key;
                var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                if (localPlayer == null)
                    continue;
                var playerChanges = lobbyPlayerChanges.Value;

                //There are changes on the Player
                if (playerChanges == null)
                    continue;

                foreach (var playerChange in playerChanges.Values)
                {
                    //There are changes on some of the changes in the player list of changes
                    Debug.LogWarning("This Sample does not remove Player Values currently.");
                }
            }
        };

        //Why注册很久导致websocket一直连接不上?可能是wss网站请求响应太慢,换公司网络如何?公司网络一切正常,那就是网络问题！！！
        await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyID, m_LobbyEventCallbacks);        
    }

    #region HeartBeat
    void StartHeartBeat()
    {
        m_HeartBeatTask = HeartBeatLoop();
    }
    async Task HeartBeatLoop()
    {
        while (m_CurrentLobby != null)
        {
            await SendHeartbeatPingAsync();
            await Task.Delay(8000);
        }
    }
    async Task SendHeartbeatPingAsync()
    {
        if (!InLobby())
            return;
        if (m_HeartBeatCooldown.IsCoolingDown)
            return;
        await m_HeartBeatCooldown.QueueUntilCooldown();

        await LobbyService.Instance.SendHeartbeatPingAsync(m_CurrentLobby.Id);
    }
    #endregion

    public async Task<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate, LocalPlayer localPlayer, string password)
    {
        if (m_CreateCooldown.IsCoolingDown)//使用IsCoolingDown,而非TaskQueued,表示不希望排队该请求任务.
        {
            Debug.LogWarning("Create Lobby hit the rate limit.");
            return null;
        }

        await m_CreateCooldown.QueueUntilCooldown();

        string uasId = AuthenticationService.Instance.PlayerId;

        CreateLobbyOptions createOptions = new CreateLobbyOptions();
        createOptions.IsPrivate = isPrivate;
        createOptions.Player = new Player(id: uasId, data: CreateInitialPlayerData(localPlayer));
        createOptions.Password = password;

        m_CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createOptions);
        StartHeartBeat();

        return m_CurrentLobby;
    }

    public async Task<Lobby> JoinLobbyAsync(string lobbyId, string lobbyCode, LocalPlayer localPlayer, string password = null)
    {
        if (m_JoinCooldown.IsCoolingDown)
        {
            Debug.LogWarning("Join Lobby hit the rate limit.");
            return null;
        }

        if (lobbyId == null && lobbyCode == null)
            return null;

        await m_JoinCooldown.QueueUntilCooldown();

        string uasId = AuthenticationService.Instance.PlayerId;
        var playerData = CreateInitialPlayerData(localPlayer);

        if (!string.IsNullOrEmpty(lobbyId))
        {
            JoinLobbyByIdOptions joinOptions = new JoinLobbyByIdOptions();
            joinOptions.Player = new Player(id: uasId, data: playerData);
            joinOptions.Password = password;

            m_CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);
        }
        else
        {
            JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions();
            joinOptions.Player = new Player(id: uasId, data: playerData);
            joinOptions.Password = password;

            m_CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);
        }

        return m_CurrentLobby;
    }

    List<QueryFilter> LobbyColorToFilters(EnumLobbyColor limitToColor)
    {
        List<QueryFilter> filters = new List<QueryFilter>();
        if (limitToColor == EnumLobbyColor.Orange)
            filters.Add(new QueryFilter(QueryFilter.FieldOptions.N1, ((int)EnumLobbyColor.Orange).ToString(), QueryFilter.OpOptions.EQ));
        else if (limitToColor == EnumLobbyColor.Green)
            filters.Add(new QueryFilter(QueryFilter.FieldOptions.N1, ((int)EnumLobbyColor.Green).ToString(), QueryFilter.OpOptions.EQ));
        else if (limitToColor == EnumLobbyColor.Blue)
            filters.Add(new QueryFilter(QueryFilter.FieldOptions.N1, ((int)EnumLobbyColor.Blue).ToString(), QueryFilter.OpOptions.EQ));
        return filters;
    }

    public async Task<Lobby> QuickJoinLobbyAsync(LocalPlayer localPlayer, EnumLobbyColor limitToColor = EnumLobbyColor.None)
    {
        if (m_QuickJoinCooldown.IsCoolingDown)
        {
            Debug.LogWarning("Quick Join Lobby hit the rate limit.");
            return null;
        }

        await m_QuickJoinCooldown.QueueUntilCooldown();
        var filters = LobbyColorToFilters(limitToColor);
        string uasId = AuthenticationService.Instance.PlayerId;

        QuickJoinLobbyOptions joinOptions = new QuickJoinLobbyOptions();
        joinOptions.Filter = filters;
        joinOptions.Player = new Player(id: uasId, data: CreateInitialPlayerData(localPlayer));

        return m_CurrentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(joinOptions);
    }

    public async Task<QueryResponse> RetrieveLobbyListAsync(EnumLobbyColor limitToColor = EnumLobbyColor.None)
    {
        var filters = LobbyColorToFilters(limitToColor);

        if (m_QueryCooldown.TaskQueued)
            return null;
        await m_QueryCooldown.QueueUntilCooldown();

        QueryLobbiesOptions queryOptions = new QueryLobbiesOptions();
        queryOptions.Count = k_maxLobbiesToShow;
        queryOptions.Filters = filters;

        return await LobbyService.Instance.QueryLobbiesAsync(queryOptions);
    }
    
    public async Task LeaveLobbyAsync()
    {
        if (!InLobby())
            return;

        //若不判断IsCooldingDown或者TaskQueued去拦截,则表明请求排队超过数量限制时,client不拦截直接发.即客户端感觉该调用重要,想其成功.
        await m_LeaveLobbyOrRemovePlayer.QueueUntilCooldown();

        string playerId = AuthenticationService.Instance.PlayerId;
        await LobbyService.Instance.RemovePlayerAsync(m_CurrentLobby.Id, playerId);
        Dispose();
    }

    //not use yet.
    public async Task<Lobby> GetLobbyAsync(string lobbyId = null)
    {
        if (!InLobby())
            return null;
        //若不判断IsCooldingDown或者TaskQueued去拦截,则表明请求排队超过数量限制时,client不拦截直接发.即客户端感觉该调用重要,想其成功.
        await m_GetLobbyCooldown.QueueUntilCooldown();

        return m_CurrentLobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
    }

    //note use yet.
    public async Task DeleteLobbyAsync()
    {
        if (!InLobby())
            return;

        //若不判断IsCooldingDown或者TaskQueued去拦截,则表明请求排队超过数量限制时,client不拦截直接发.即客户端感觉该调用重要,想其成功.
        await m_DeleteLobbyCooldown.QueueUntilCooldown();

        await LobbyService.Instance.DeleteLobbyAsync(m_CurrentLobby.Id);
    }

    public async Task UpdateLobbyDataAsync(Dictionary<string, string> datas)
    {
        if (!InLobby())
            return;

        Dictionary<string, DataObject> dataCurr = m_CurrentLobby.Data ?? new Dictionary<string, DataObject>();

        var isLocked = false;
        foreach (var dataItem in datas)
        {
            //setup dataCurr.
            DataObject.IndexOptions indexOptions = dataItem.Key == key_LobbyColor ? DataObject.IndexOptions.N1 : 0;
            DataObject dataObject = new DataObject(DataObject.VisibilityOptions.Public, dataItem.Value, indexOptions);
            if (dataCurr.ContainsKey(dataItem.Key))
            {
                dataCurr[dataItem.Key] = dataObject;
            }
            else
            {
                dataCurr.Add(dataItem.Key, dataObject);
            }

            //set isLocked.
            if (dataItem.Key == key_LobbyState)
            {
                Enum.TryParse(dataItem.Value, out EnumLobbyState enumLobbyState);
                isLocked = enumLobbyState != EnumLobbyState.Lobby;
            }
        }

        if (m_UpdateLobbyCooldown.TaskQueued)
            return;
        await m_UpdateLobbyCooldown.QueueUntilCooldown();

        UpdateLobbyOptions updateOptions = new UpdateLobbyOptions { Data = dataCurr, IsLocked = isLocked };
        m_CurrentLobby = await LobbyService.Instance.UpdateLobbyAsync(m_CurrentLobby.Id, updateOptions);
    }

    public async Task UpdatePlayerDataAsync(Dictionary<string, string> datas)
    {
        if (!InLobby())
            return;

        string playerId = AuthenticationService.Instance.PlayerId;
        Dictionary<string, PlayerDataObject> dataCurr = new Dictionary<string, PlayerDataObject>();
        foreach (var dataItem in datas)
        {
            PlayerDataObject dataObj = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, dataItem.Value);
            if (dataCurr.ContainsKey(dataItem.Key))
            {
                dataCurr[dataItem.Key] = dataObj;
            }
            else
            {
                dataCurr.Add(dataItem.Key, dataObj);
            }
        }

        if (m_UpdatePlayerCooldown.TaskQueued)
            return;
        await m_UpdatePlayerCooldown.QueueUntilCooldown();

        UpdatePlayerOptions updatePlayerOptions = new UpdatePlayerOptions();
        updatePlayerOptions.Data = dataCurr;
        updatePlayerOptions.AllocationId = null;
        updatePlayerOptions.ConnectionInfo = null;
        m_CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(m_CurrentLobby.Id, playerId, updatePlayerOptions);
    }

    //not use yet.
    public async Task UpdatePlayerRelayInfoAsync(Dictionary<string, string> datas)
    {
        if (!InLobby())
            return;

        string playerId = AuthenticationService.Instance.PlayerId;

        if (m_UpdateLobbyCooldown.TaskQueued)
        {
            return;
        }
        await m_UpdateLobbyCooldown.QueueUntilCooldown();

    }

    public void Dispose()
    {
        m_CurrentLobby = null;
        m_LobbyEventCallbacks = new LobbyEventCallbacks();
    }
}
