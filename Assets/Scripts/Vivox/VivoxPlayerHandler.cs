using Unity.Services.Vivox;
using UnityEngine;
using VivoxUnity;

/// <summary>
/// 监听大厅中一个玩家的Vivox状态改变.
/// 这里使用Vivox服务而不是通过Relay,是因为它已经对所有玩家传输了状态改变.
/// </summary>
public class VivoxPlayerHandler : MonoBehaviour
{
    [SerializeField]
    private LobbyPlayerVolumeUI m_lobbyPlayerVolumeUI;

    private IChannelSession m_channelSession;
    private string m_id;
    private string m_vivoxId;

    private const int k_volumeMin = -50, k_volumeMax = 20;//从Vivox文档看,有效范围是[-50,50],但超过25可能发出刺耳的噪音.

    public static float NormalizedVolumeDefault
    {
        get { return (0f - k_volumeMin) / (k_volumeMax - k_volumeMin); }
    }

    public void Start()
    {
        m_lobbyPlayerVolumeUI.DisableVoice(true);
    }

    public void SetId(string id)
    {
        m_id = id;

        m_vivoxId = null;

        if (m_channelSession != null)
        {
            foreach (var participant in m_channelSession.Participants)
            {
                if (m_id == participant.Account.DisplayName)
                {
                    m_vivoxId = participant.Key;
                    m_lobbyPlayerVolumeUI.IsLocalPlayer = participant.IsSelf;
                    m_lobbyPlayerVolumeUI.EnableVoice(true);
                    break;
                }
            }
        }
    }

    public void OnChannelJoined(IChannelSession channelSession)//加入大厅后开始连接,连接确认时调用.
    {
        //todo:检查是否静音

        m_channelSession = channelSession;
        m_channelSession.Participants.AfterKeyAdded += OnParticipantAdded;
        m_channelSession.Participants.BeforeKeyRemoved += BeforeParticipantRemoved;
        m_channelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
    }

    public void OnChannelLeft()//离开大厅时调用
    {
        if (m_channelSession != null)//可能存在离开一个还未加入的channel,例如离开一个Vivox正在连接中的大厅.
        {
            m_channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
            m_channelSession.Participants.BeforeKeyRemoved -= BeforeParticipantRemoved;
            m_channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;
        }
    }

    private void OnParticipantAdded(object sender, KeyEventArg<string> e)
    {
        var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        var participant = source[e.Key];
        var displayName = participant.Account.DisplayName;

        bool isCurrentPlayer = displayName == m_id;
        if (isCurrentPlayer)
        {
            m_vivoxId = e.Key;//由于无法更早的构造VivoxID,这里设置它.
            m_lobbyPlayerVolumeUI.IsLocalPlayer = participant.IsSelf;

            if (participant.IsMutedForAll)
                m_lobbyPlayerVolumeUI.DisableVoice(false);
            else
                m_lobbyPlayerVolumeUI.EnableVoice(false);//应该检查用户是否被禁言了
        }
        else
        {
            if (participant.LocalMute)
                m_lobbyPlayerVolumeUI.DisableVoice(false);
            else
                m_lobbyPlayerVolumeUI.EnableVoice(false);//应该检查用户是否被禁言了
        }
    }

    private void OnParticipantValueUpdated(object sender, ValueEventArg<string, IParticipant> e)
    {
        var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        var participant = source[e.Key];
        var displayName = participant.Account.DisplayName;
        string propertyName = e.PropertyName;

        if (displayName == m_id)
        {
            if (propertyName == "UnavailableCaptureDevice")
            {
                if (participant.UnavailableCaptureDevice)
                {
                    m_lobbyPlayerVolumeUI.DisableVoice(false);
                    participant.SetIsMuteForAll(true, null);//注意:如果你需要添加更多一个玩家全局静音的地方,使用状态机可能更能精确的控制逻辑.
                }
                else
                {
                    m_lobbyPlayerVolumeUI.EnableVoice(false);
                    participant.SetIsMuteForAll(false, null);//需要注意的是这个调用是异步的,因此在其完成前可能退出了lobby,将会导致一个Vivox错误.
                }
            }
            else if (propertyName == "IsMutedForAll")
            {
                if (participant.IsMutedForAll)
                    m_lobbyPlayerVolumeUI.DisableVoice(false);
                else
                    m_lobbyPlayerVolumeUI.EnableVoice(false);
            }
        }
    }

    private void BeforeParticipantRemoved(object sender, KeyEventArg<string> e)
    {
        var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        var participant = source[e.Key];
        var displayName = participant.Account.DisplayName;

        if (displayName == m_id)
            m_lobbyPlayerVolumeUI.DisableVoice(true);
    }

    public void OnVolumeSlide(float volumeNormalized)
    {
        if (m_channelSession == null || m_vivoxId == null)//验证是否初始化,由于对本地和远程客户端,SetId和OnChannelJoined被调用在不同的时间.
            return;

        int vol = (int)Mathf.Clamp(k_volumeMin + (k_volumeMax - k_volumeMin) * volumeNormalized, k_volumeMin, k_volumeMax);
        bool isSelf = m_channelSession.Participants[m_vivoxId].IsSelf;
        if (isSelf)
        {
            VivoxService.Instance.Client.AudioInputDevices.VolumeAdjustment = vol;
        }
        else
        {
            m_channelSession.Participants[m_vivoxId].LocalVolumeAdjustment = vol;
        }
    }

    public void OnMuteToggle(bool isMuted)
    {
        if (m_channelSession == null || m_vivoxId == null)
            return;

        bool isSelf = m_channelSession.Participants[m_vivoxId].IsSelf;
        if (isSelf)
        {
            VivoxService.Instance.Client.AudioInputDevices.Muted = isMuted;
        }
        else
        {
            m_channelSession.Participants[m_vivoxId].LocalMute = isMuted;
        }
    }
}