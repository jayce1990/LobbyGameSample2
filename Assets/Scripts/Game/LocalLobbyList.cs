using System;
using System.Collections.Generic;

/// <summary>
/// 大厅列表中使用,用于表明何时处于大厅检索中.
/// </summary>
public enum EnumLobbyQueryState
{
    Empty,
    Fetching,
    Error,
    Fetched
}

/// <summary>
/// 持有与Lobby服务本身相关的数据 - 最新检索到的Lobby列表、检索状态。
/// </summary>
[Serializable]
public class LocalLobbyList
{
    public CallbackValue<EnumLobbyQueryState> QueryState = new CallbackValue<EnumLobbyQueryState>();

    public Action<Dictionary<string, LocalLobby>> onLobbyListChange;
    Dictionary<string, LocalLobby> m_currentLobbies = new Dictionary<string, LocalLobby>();

    public Dictionary<string, LocalLobby> CurrentLobbies
    {
        get { return m_currentLobbies; }
        set
        {
            m_currentLobbies = value;
            onLobbyListChange?.Invoke(m_currentLobbies);
        }
    }

    public void Clear()
    {
        m_currentLobbies.Clear();
        QueryState.Value = EnumLobbyQueryState.Fetched;
    }
}
