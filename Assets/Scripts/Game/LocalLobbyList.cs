using System;
using System.Collections.Generic;

/// <summary>
/// �����б���ʹ��,���ڱ�����ʱ���ڴ���������.
/// </summary>
public enum EnumLobbyQueryState
{
    Empty,
    Fetching,
    Error,
    Fetched
}

/// <summary>
/// ������Lobby��������ص����� - ���¼�������Lobby�б�����״̬��
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
