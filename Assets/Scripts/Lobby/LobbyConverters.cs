using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;

/// <summary>
/// ���غ�Զ��Lobby��User��ת��(������)
/// </summary>
public class LobbyConverters 
{
    const string key_RelayCode = nameof(LocalLobby.RelayCode);
    const string key_LobbyState = nameof(LocalLobby.LocalLobbyState);
    const string key_LobbyColor = nameof(LocalLobby.LocalLobbyColor);
    const string key_LastEdit = nameof(LocalLobby.LastUpdated);

    const string key_Displayname = nameof(LocalPlayer.DisplayName);
    const string key_Emote = nameof(LocalPlayer.Emote);
    const string key_PlayerStatus = nameof(LocalPlayer.PlayerStatus);

    public static Dictionary<string, string> LocalToRemoteLobbyData(LocalLobby lobby)
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add(key_RelayCode, lobby.RelayCode.Value);
        data.Add(key_LobbyState, ((int)lobby.LocalLobbyState.Value).ToString());
        data.Add(key_LobbyColor, ((int)lobby.LocalLobbyColor.Value).ToString());
        data.Add(key_LastEdit, lobby.LastUpdated.Value.ToString());

        return data;
    }

    public static Dictionary<string, string> LocalToRemotePlayerData(LocalPlayer player)
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add(key_Displayname, player.DisplayName.Value);
        data.Add(key_Emote, ((int)player.Emote.Value).ToString());
        data.Add(key_PlayerStatus, ((int)player.PlayerStatus.Value).ToString());
        return data;
    }

    /// <summary>
    /// ����������Lobbyʱ:����Զ��lobby��������,����һ���µı���lobby
    /// </summary>
    public static void RemoteToLocal(Lobby remoteLobby, LocalLobby localLobby)
    {
        if (remoteLobby == null)
        {
            Debug.LogError("remoteLobby is null, cannot convert.");
            return;
        }
        if (localLobby == null)
        {
            Debug.LogError("localLobby is null, cannot convert.");
            return;
        }

        localLobby.LobbyID.Value = remoteLobby.Id;
        localLobby.LobbyCode.Value = remoteLobby.LobbyCode;
        localLobby.LobbyName.Value = remoteLobby.Name;
        localLobby.HostID.Value = remoteLobby.HostId;
        localLobby.Locked.Value = remoteLobby.IsLocked;
        localLobby.Private.Value = remoteLobby.IsPrivate;
        localLobby.AvailableSlots.Value = remoteLobby.AvailableSlots;
        localLobby.MaxPlayerCount.Value = remoteLobby.MaxPlayers;
        localLobby.LastUpdated.Value = remoteLobby.LastUpdated.ToFileTimeUtc();

        //�Զ���Lobby����ת��
        localLobby.RelayCode.Value = remoteLobby.Data?.ContainsKey(key_RelayCode) == true ? remoteLobby.Data[key_RelayCode].Value : localLobby.RelayCode.Value;
        localLobby.LocalLobbyState.Value = remoteLobby.Data?.ContainsKey(key_LobbyState) == true ? (EnumLobbyState)int.Parse(remoteLobby.Data[key_LobbyState].Value) : EnumLobbyState.Lobby;
        localLobby.LocalLobbyColor.Value = remoteLobby.Data?.ContainsKey(key_LobbyColor) == true ? (EnumLobbyColor)int.Parse(remoteLobby.Data[key_LobbyColor].Value) : EnumLobbyColor.None;

        //�Զ���User����ת��
        int index = 0;
        foreach (var player in remoteLobby.Players)
        {
            var id = player.Id;
            var isHost = remoteLobby.HostId.Equals(player.Id);
            var displayName = player.Data?.ContainsKey(key_Displayname) == true ? player.Data[key_Displayname].Value : default;
            var emote = player.Data?.ContainsKey(key_Emote) == true ? (EnumEmoteType)int.Parse(player.Data[key_Emote].Value) : EnumEmoteType.None;
            var PlayerStatus = player.Data?.ContainsKey(key_PlayerStatus) == true ? (EnumPlayerStatus)int.Parse(player.Data[key_PlayerStatus].Value) : EnumPlayerStatus.Lobby;            

            LocalPlayer localPlayer = localLobby.GetLocalPlayer(index);
            if (localPlayer == null)
            {
                localPlayer = new LocalPlayer(id, index, isHost, displayName, emote, PlayerStatus);
                localLobby.AddPlayer(index, localPlayer);//�ս��뷿��,��ʼ���������.
            }
            else
            {
                localPlayer.ID.Value = id;
                localPlayer.Index.Value = index;
                localPlayer.IsHost.Value = isHost;
                localPlayer.DisplayName.Value = displayName;
                localPlayer.Emote.Value = emote;
                localPlayer.PlayerStatus.Value = PlayerStatus;
            }

            index++;
        }
    }

    /// <summary>
    /// �Ӵ����б��ѯ�����,����һ���µĴ����б�.
    /// </summary>
    public static List<LocalLobby> QueryToLocalList(QueryResponse response)
    {
        List<LocalLobby> retList = new List<LocalLobby>();
        foreach (var lobby in response.Results)
            retList.Add(RemoteToNewLocal(lobby));
        return retList;
    }

    static LocalLobby RemoteToNewLocal(Lobby lobby)
    {
        LocalLobby data = new LocalLobby();
        RemoteToLocal(lobby, data);
        return data;
    }
}