using System;
using System.Collections.Generic;
using System.Text;

[Flags]//һЩUIԪ����ָ��ͬʱ�ڼ����еĶ��״̬,��������Flags
public enum EnumLobbyState
{
    Lobby = 1,
    CountDown = 2,
    InGame = 4
}

/// <summary>
/// ������ɫ(��������չΪ��ͼ��ģʽ��),���ڹ���������ѯ�����б�
/// </summary>
public enum EnumLobbyColor
{
    None = 0,
    Orange = 1,
    Green = 2,
    Blue = 3
}

/// <summary>
/// Χ�ƴ���Զ�����ݵı��ذ�װ��,������UIԪ���ṩ���ݺ͸��ٱ�����Ҷ���ĸ��ӹ���.
/// ���������������ݲ�һ���������ǵ�����,���������Ҫ����ӳ�䵽��LocalLobby,�Ա������ʹ��.
/// </summary>
[Serializable]
public class LocalLobby
{
    public Action<LocalPlayer> onUserJoined;

    public Action<int> onUserLeft;

    public Action<int> onUserReadyChange;

    public CallbackValue<string> LobbyID = new CallbackValue<string>();

    public CallbackValue<string> LobbyCode = new CallbackValue<string>();

    public CallbackValue<string> RelayCode = new CallbackValue<string>();

    public CallbackValue<string> LobbyName = new CallbackValue<string>();

    public CallbackValue<string> HostID = new CallbackValue<string>();

    public CallbackValue<EnumLobbyState> LocalLobbyState = new CallbackValue<EnumLobbyState>();

    public CallbackValue<bool> Locked = new CallbackValue<bool>();

    public CallbackValue<bool> Private = new CallbackValue<bool>();

    public CallbackValue<int> AvailableSlots = new CallbackValue<int>();

    public CallbackValue<int> MaxPlayerCount = new CallbackValue<int>();

    public CallbackValue<EnumLobbyColor> LocalLobbyColor = new CallbackValue<EnumLobbyColor>();

    public CallbackValue<long> LastUpdated = new CallbackValue<long>();

    public CallbackValue<ServerAddress> RelayServer = new CallbackValue<ServerAddress>();

    public int PlayerCount => m_LocalPlayers.Count;
    public List<LocalPlayer> LocalPlayers => m_LocalPlayers;
    List<LocalPlayer> m_LocalPlayers = new List<LocalPlayer>();

    public void ResetLobby()
    {        
        onUserJoined = null;
        onUserLeft = null;
        onUserReadyChange = null;
        LobbyID.Value = "";
        LobbyName.Value = "";
        RelayCode.Value = "";        
        LobbyName.Value = "";
        HostID.Value = "";
        LocalLobbyState.Value = EnumLobbyState.Lobby;
        Locked.Value = false;
        Private.Value = false;
        AvailableSlots.Value = 4;
        MaxPlayerCount.Value = 4;
        LocalLobbyColor.Value = EnumLobbyColor.None;
        LastUpdated.Value = 0;
        m_LocalPlayers.Clear();
        //RelayServer = null;
    }

    public LocalLobby()
    {
        LastUpdated.Value = DateTime.Now.ToFileTimeUtc();
        HostID.onChanged += OnHostChanged;
    }

    ~LocalLobby()
    {
        HostID.onChanged -= OnHostChanged;
    }

    private void OnHostChanged(string newHostId)
    {
        foreach (var player in m_LocalPlayers)
        {
            player.IsHost.Value = player.ID.Value == newHostId;
        }
    }

    public LocalPlayer GetLocalPlayer(int index)
    {
        return PlayerCount > index ? m_LocalPlayers[index] : null;
    }

    public void AddPlayer(int index, LocalPlayer user)
    {
        m_LocalPlayers.Insert(index, user);
        user.PlayerStatus.onChanged += OnPlayerStatusChanged;
        onUserJoined?.Invoke(user);
    }

    public void RemovePlayer(int playerIndex)
    {
        m_LocalPlayers[playerIndex].PlayerStatus.onChanged -= OnPlayerStatusChanged;
        m_LocalPlayers.RemoveAt(playerIndex);
        onUserLeft?.Invoke(playerIndex);
    }

    void OnPlayerStatusChanged(EnumPlayerStatus PlayerStatus)
    {
        int readyCount = 0;
        foreach (var player in m_LocalPlayers)
        {
            if (player.PlayerStatus.Value == EnumPlayerStatus.Ready)
            {
                readyCount++;
            }
        }

        onUserReadyChange?.Invoke(readyCount);
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("Lobby: ");
        sb.AppendLine(LobbyName.Value);
        sb.Append("ID: ");
        sb.AppendLine(LobbyID.Value);
        sb.Append("Code: ");
        sb.AppendLine(LobbyCode.Value);
        sb.Append("Locked: ");
        sb.AppendLine(Locked.Value.ToString());
        sb.Append("Private: ");
        sb.AppendLine(Private.Value.ToString());
        sb.Append("AvailableSlots: ");
        sb.AppendLine(AvailableSlots.Value.ToString());
        sb.Append("Max Players: ");
        sb.AppendLine(MaxPlayerCount.Value.ToString());
        sb.Append("LocalLobbyState: ");
        sb.AppendLine(LocalLobbyState.Value.ToString());
        sb.Append("Lobby LocalLobbyState Last Edit: ");
        sb.AppendLine(new DateTime(LastUpdated.Value).ToString());
        sb.Append("LocalLobbyColor: ");
        sb.AppendLine(LocalLobbyColor.Value.ToString());
        sb.Append("RelayCode: ");
        sb.AppendLine(RelayCode.Value);

        return sb.ToString();
    }
}
