using TMPro;
using UnityEngine;

public class RelayAddressUI : UIPanelBase
{
    [SerializeField]
    TMP_Text m_IPAddressText;

    public override void Start()
    {
        base.Start();
        GameManager.Instance.LocalLobby.RelayServer.onChanged += GotRelayAddress;
    }

    void GotRelayAddress(ServerAddress address)
    {
        m_IPAddressText.SetText(address.ToString());
    }
}
