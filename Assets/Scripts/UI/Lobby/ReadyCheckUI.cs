public class ReadyCheckUI : UIPanelBase
{
    public void OnReadyButton()
    {
        Manager.SetLocalPlayerStatus(EnumPlayerStatus.Ready);
    }

    public void OnCancelButton()
    {
        Manager.SetLocalPlayerStatus(EnumPlayerStatus.Lobby);
    }
}
