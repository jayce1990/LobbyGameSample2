using System;

/// <summary>
/// �������û��ĵ�ǰ״̬
/// ����һ��Flagsö��,������������ѡ,��ʵ�ָ���UI����
/// </summary>
[Flags]
public enum EnumPlayerStatus
{
    None = 0,
    Connecting = 1, //�Ѽ���Lobby,����δ����Relay
    Lobby = 2, //��Lobby�������ӵ�Relay
    Ready = 4, //�û��Ѿ�ѡ����׼����ť,Ϊ��׼��"��Ϸ"��ʼ
    InGame = 8, //�û����ѿ�ʼ"��Ϸ"��һ����
    Menu = 16 //�û�����Lobby,��ĳһ���˵�
}

/// <summary>
/// �����������,���ᱻ���²����۲�,֪����ʱ������Ҹ��µ���������Lobby
/// </summary>
[Serializable]
public class LocalPlayer
{
    public CallbackValue<string> ID = new CallbackValue<string>("");
    public CallbackValue<int> Index = new CallbackValue<int>(0);
    public CallbackValue<string> DisplayName = new CallbackValue<string>("");
    public CallbackValue<bool> IsHost = new CallbackValue<bool>(false);
    public CallbackValue<EnumEmoteType> Emote = new CallbackValue<EnumEmoteType>(EnumEmoteType.None);
    public CallbackValue<EnumPlayerStatus> PlayerStatus = new CallbackValue<EnumPlayerStatus>((EnumPlayerStatus)0);


    public DateTime LastUpdated;

    public LocalPlayer(string id, int index, bool isHost, string displayName = default, EnumEmoteType emote = default, EnumPlayerStatus status = default)
    {
        ID.Value = id;
        Index.Value = index;
        DisplayName.Value = displayName;
        IsHost.Value = isHost;
        Emote.Value = emote;
        PlayerStatus.Value = status;
    }

    /// <summary>
    /// ��������뿪����ʱ��������
    /// </summary>
    public void ResetState()
    {
        IsHost.Value = false;
        Emote.Value = EnumEmoteType.None;
        PlayerStatus.Value = EnumPlayerStatus.Menu;
    }
}
