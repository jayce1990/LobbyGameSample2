using System;

/// <summary>
/// 大厅中用户的当前状态
/// 这是一个Flags枚举,用于属性面板多选,以实现各种UI功能
/// </summary>
[Flags]
public enum EnumPlayerStatus
{
    None = 0,
    Connecting = 1, //已加入Lobby,但还未连接Relay
    Lobby = 2, //在Lobby且已连接到Relay
    Ready = 4, //用户已经选择了准备按钮,为了准备"游戏"开始
    InGame = 8, //用户是已开始"游戏"的一部分
    Menu = 16 //用户不在Lobby,在某一主菜单
}

/// <summary>
/// 本地玩家数据,将会被更新并被观察,知道何时推送玩家更新到整个大厅Lobby
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
    /// 本地玩家离开大厅时重置数据
    /// </summary>
    public void ResetState()
    {
        IsHost.Value = false;
        Emote.Value = EnumEmoteType.None;
        PlayerStatus.Value = EnumPlayerStatus.Menu;
    }
}
