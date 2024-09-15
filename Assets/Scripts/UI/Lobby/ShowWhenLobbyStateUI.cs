using UnityEngine;

public class ShowWhenLobbyStateUI : UIPanelBase
{
    [SerializeField]
    EnumLobbyState m_ShowThisWhen;

    public override void Start()
    {
        base.Start();

        Manager.LocalLobby.LocalLobbyState.onChanged += (state) => 
        {
            if (m_ShowThisWhen.HasFlag(state))
                Show();
            else
                Hide();
        };
    }
}
