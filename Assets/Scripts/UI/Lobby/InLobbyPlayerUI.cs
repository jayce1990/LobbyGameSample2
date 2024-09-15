using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InLobbyPlayerUI : UIPanelBase
{
    [SerializeField]
    TMP_Text m_DispalyNameText;
    [SerializeField]
    TMP_Text m_StatusText;
    [SerializeField]
    Image m_HostIcon;
    [SerializeField]
    Image m_EmoteImage;
    [SerializeField]
    Sprite[] m_EmoteIcons;
    [SerializeField]
    VivoxPlayerHandler m_VivoxPlayerHandler;

    public bool IsAssigned => PlayerId != null;
    public string PlayerId { get; set; }
    LocalPlayer m_LocalPlayer;

    public void SetPlayer(LocalPlayer localPlayer)
    {
        Show();
        m_LocalPlayer = localPlayer;
        PlayerId = m_LocalPlayer.ID.Value;
        SetIsHost(localPlayer.IsHost.Value);
        SetEmote(localPlayer.Emote.Value);
        SetPlayerStatus(localPlayer.PlayerStatus.Value);
        SetDisplayName(m_LocalPlayer.DisplayName.Value);
        SubscribeToPlayerUpdates();        

        m_VivoxPlayerHandler.SetId(PlayerId);
    }

    public void ResetUI()
    {
        if (m_LocalPlayer == null)
            return;
        PlayerId = null;
        SetEmote(EnumEmoteType.None);
        SetPlayerStatus(EnumPlayerStatus.Lobby);
        Hide();
        UnsubscribeToPlayerUpdates();
        m_LocalPlayer = null;
    }

    void SubscribeToPlayerUpdates()
    {
        m_LocalPlayer.IsHost.onChanged += SetIsHost;
        m_LocalPlayer.Emote.onChanged += SetEmote;
        m_LocalPlayer.PlayerStatus.onChanged += SetPlayerStatus;
        m_LocalPlayer.DisplayName.onChanged += SetDisplayName;
    }
    void UnsubscribeToPlayerUpdates()
    {
        if (m_LocalPlayer == null)
            return;
        if (m_LocalPlayer.IsHost.onChanged != null)
            m_LocalPlayer.IsHost.onChanged -= SetIsHost;
        if (m_LocalPlayer.Emote.onChanged != null)
            m_LocalPlayer.Emote.onChanged -= SetEmote;
        if (m_LocalPlayer.PlayerStatus.onChanged != null)
            m_LocalPlayer.PlayerStatus.onChanged -= SetPlayerStatus;
        if (m_LocalPlayer.DisplayName.onChanged != null)
            m_LocalPlayer.DisplayName.onChanged -= SetDisplayName;
    }

    void SetIsHost(bool isHost)
    {
        m_HostIcon.enabled = isHost;
    }
    void SetEmote(EnumEmoteType emoteType) 
    {
        /// <summary>
        /// EmoteType to Icon Sprite
        /// m_EmoteIcon[0] = Smile
        /// m_EmoteIcon[1] = Frown
        /// m_EmoteIcon[2] = UnAmused
        /// m_EmoteIcon[3] = Tongue
        /// </summary>
        switch (emoteType)
        {
            case EnumEmoteType.None:
                m_EmoteImage.color = Color.clear;
                m_EmoteImage.sprite = null;
                break;
            case EnumEmoteType.Smile:
                m_EmoteImage.color = Color.white;
                m_EmoteImage.sprite = m_EmoteIcons[0];
                break;
            case EnumEmoteType.Frown:
                m_EmoteImage.color = Color.white;
                m_EmoteImage.sprite = m_EmoteIcons[1];
                break;
            case EnumEmoteType.Unamused:
                m_EmoteImage.color = Color.white;
                m_EmoteImage.sprite = m_EmoteIcons[2];
                break;
            case EnumEmoteType.Tongue:
                m_EmoteImage.color = Color.white;
                m_EmoteImage.sprite = m_EmoteIcons[3];
                break;
            default:
                m_EmoteImage.sprite = null;
                break;
        }
      
    }
    void SetPlayerStatus(EnumPlayerStatus playerStatus)
    {
        switch (playerStatus)
        {
            case EnumPlayerStatus.Lobby:
                m_StatusText.SetText("<color=#56B4E9>In Lobby</color>"); // Light Blue
                break;
            case EnumPlayerStatus.Ready:
                m_StatusText.SetText("<color=#009E73>Ready</color>"); // Light Mint
                break;
            case EnumPlayerStatus.Connecting:
                m_StatusText.SetText("<color=#F0E442>Connecting...</color>"); // Bright Yellow
                break;
            case EnumPlayerStatus.InGame:
                m_StatusText.SetText("<color=#005500>In Game</color>"); // Green
                break;
            default:
                m_StatusText.SetText("");
                break;
        }
    }
    void SetDisplayName(string displayName)
    {
        m_DispalyNameText.SetText(displayName);
    }


}
