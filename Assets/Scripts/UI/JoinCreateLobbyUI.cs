using UnityEngine;
using UnityEngine.Events;

public enum EnumJoinCreateTabs
{
    Join,
    Create
}

public class JoinCreateLobbyUI : UIPanelBase
{
    [HideInInspector]
    public UnityEvent<EnumJoinCreateTabs> m_OnTabChanged;

    [SerializeField]
    EnumJoinCreateTabs m_CurrentTab = EnumJoinCreateTabs.Join;

    EnumJoinCreateTabs CurrentTab
    {
        get => m_CurrentTab;
        set
        {
            m_CurrentTab = value;
            m_OnTabChanged?.Invoke(m_CurrentTab);
        }
    }

    public override void Start()
    {
        base.Start();
        Manager.onGameStateChanged += GameStateChanged;
    }

    void GameStateChanged(GameState state)
    {
        if (state == GameState.JoinMenu)
        {
            m_OnTabChanged?.Invoke(m_CurrentTab);//��ʼ��:��֮ǰ��ѡ��Tab,֪ͨJoin��Create������ʾ������.

            Show(false);
        }
        else
        {
            Hide();
        }
    }

    public void SetJoinTab()//UI����:�л�Tabҳ,֪ͨJoin��Create������ʾ������.
    {
        CurrentTab = EnumJoinCreateTabs.Join;
    }

    public void SetCreateTab()//ͬ��
    {
        CurrentTab = EnumJoinCreateTabs.Create;
    }

    private void OnDestroy()
    {
        if (Manager != null)
            Manager.onGameStateChanged -= GameStateChanged;
    }
}
