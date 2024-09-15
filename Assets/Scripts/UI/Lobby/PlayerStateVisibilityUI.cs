using System;

[Flags]
public enum PlayerPremission
{
    Client = 1,
    Host = 2
}

public class PlayerStateVisibilityUI : UIPanelBase
{
    public EnumPlayerStatus ShowThisWhen;
    public PlayerPremission Premissions;
    bool m_HasStatusFlags = false;
    bool m_HasPermissions;

    public override async void Start()
    {
        base.Start();
        var localPlayer = await Manager.AwaitLocalPlayerInitialization();
        localPlayer.IsHost.onChanged += OnPlayerHostChanged;
        localPlayer.PlayerStatus.onChanged += OnPlayerStatusChanged;
    }

    void OnPlayerHostChanged(bool isHost)
    {
        m_HasPermissions = isHost ? Premissions.HasFlag(PlayerPremission.Host) : Premissions.HasFlag(PlayerPremission.Client);
        CheckVisibility();
    }
    void OnPlayerStatusChanged(EnumPlayerStatus observedStatus)
    {
        m_HasStatusFlags = ShowThisWhen.HasFlag(observedStatus);
        CheckVisibility();
    }
    void CheckVisibility()
    {
        if (m_HasStatusFlags && m_HasPermissions)
            Show();
        else
            Hide();
    }
}
