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
/// ��LobbyAPI��ֱ�ӵ��ú���ֱ����Ҫ����ĳ����.
/// ����,���������ȡһ���ɶ����б�,������Ҫֱ�ӵĲ�ѯ����.
/// </summary>
/// 
/// ͬһʱ��ֻ����һ������,ֻ��ͨ��JoinAsync,CreateAsync,��QuickJoinAsync���ܽ������ID�Ĵ���.

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
    const int k_maxLobbiesToShow = 16;//���Ҫ����,���ǻ�ȡ��ҳ�Ľ������ʹ�ù���

    Task m_HeartBeatTask;

    //����UI[RateLimitVisibility���]ѡ���EnumRequestType����ӦServiceRateLimiter����ȴ״̬�ı��¼�,����UI�ڵ������.
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
    //RateLimits��������: https://docs.unity.com/lobby/rate-limits.html
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
            Debug.LogWarning("LobbyManager��ǰ����һ��������,�Ƿ�CreateLobbyAsync or JoinLobbyAsync?");
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

    //��Զ�̴���������¼������ݸı��¼�,�󶨵����صĴ�������ҵ����ݸı�.
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
        m_LobbyEventCallbacks.LobbyEventConnectionStateChanged += lobbyEventConnectionState => { Debug.Log($"��������״̬���{lobbyEventConnectionState}."); };

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

        //Whyע��ܾõ���websocketһֱ���Ӳ���?������wss��վ������Ӧ̫��,����˾�������?��˾����һ������,�Ǿ����������⣡����
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
        if (m_CreateCooldown.IsCoolingDown)//ʹ��IsCoolingDown,����TaskQueued,��ʾ��ϣ���ŶӸ���������.
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

        //�����ж�IsCooldingDown����TaskQueuedȥ����,����������Ŷӳ�����������ʱ,client������ֱ�ӷ�.���ͻ��˸о��õ�����Ҫ,����ɹ�.
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
        //�����ж�IsCooldingDown����TaskQueuedȥ����,����������Ŷӳ�����������ʱ,client������ֱ�ӷ�.���ͻ��˸о��õ�����Ҫ,����ɹ�.
        await m_GetLobbyCooldown.QueueUntilCooldown();

        return m_CurrentLobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
    }

    //note use yet.
    public async Task DeleteLobbyAsync()
    {
        if (!InLobby())
            return;

        //�����ж�IsCooldingDown����TaskQueuedȥ����,����������Ŷӳ�����������ʱ,client������ֱ�ӷ�.���ͻ��˸о��õ�����Ҫ,����ɹ�.
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
