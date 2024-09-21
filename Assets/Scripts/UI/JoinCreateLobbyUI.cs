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
            m_OnTabChanged?.Invoke(m_CurrentTab);//初始化:将之前的选择Tab,通知Join和Create进行显示和隐藏.

            Show(false);
        }
        else
        {
            Hide();
        }
    }

    public void SetJoinTab()//UI操作:切换Tab页,通知Join和Create进行显示和隐藏.
    {
        CurrentTab = EnumJoinCreateTabs.Join;
    }

    public void SetCreateTab()//同上
    {
        CurrentTab = EnumJoinCreateTabs.Create;
    }

    private void OnDestroy()
    {
        if (Manager != null)
            Manager.onGameStateChanged -= GameStateChanged;
    }
}
