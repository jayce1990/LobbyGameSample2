using Unity.Services.Vivox;
using UnityEngine;
using VivoxUnity;

/// <summary>
/// ����������һ����ҵ�Vivox״̬�ı�.
/// ����ʹ��Vivox���������ͨ��Relay,����Ϊ���Ѿ���������Ҵ�����״̬�ı�.
/// </summary>
public class VivoxPlayerHandler : MonoBehaviour
{
    [SerializeField]
    private LobbyPlayerVolumeUI m_lobbyPlayerVolumeUI;

    private IChannelSession m_channelSession;
    private string m_id;
    private string m_vivoxId;

    private const int k_volumeMin = -50, k_volumeMax = 20;//��Vivox�ĵ���,��Ч��Χ��[-50,50],������25���ܷ����̶�������.

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

    public void OnChannelJoined(IChannelSession channelSession)//���������ʼ����,����ȷ��ʱ����.
    {
        //todo:����Ƿ���

        m_channelSession = channelSession;
        m_channelSession.Participants.AfterKeyAdded += OnParticipantAdded;
        m_channelSession.Participants.BeforeKeyRemoved += BeforeParticipantRemoved;
        m_channelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
    }

    public void OnChannelLeft()//�뿪����ʱ����
    {
        if (m_channelSession != null)//���ܴ����뿪һ����δ�����channel,�����뿪һ��Vivox���������еĴ���.
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
            m_vivoxId = e.Key;//�����޷�����Ĺ���VivoxID,����������.
            m_lobbyPlayerVolumeUI.IsLocalPlayer = participant.IsSelf;

            if (participant.IsMutedForAll)
                m_lobbyPlayerVolumeUI.DisableVoice(false);
            else
                m_lobbyPlayerVolumeUI.EnableVoice(false);//Ӧ�ü���û��Ƿ񱻽�����
        }
        else
        {
            if (participant.LocalMute)
                m_lobbyPlayerVolumeUI.DisableVoice(false);
            else
                m_lobbyPlayerVolumeUI.EnableVoice(false);//Ӧ�ü���û��Ƿ񱻽�����
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
                    participant.SetIsMuteForAll(true, null);//ע��:�������Ҫ��Ӹ���һ�����ȫ�־����ĵط�,ʹ��״̬�����ܸ��ܾ�ȷ�Ŀ����߼�.
                }
                else
                {
                    m_lobbyPlayerVolumeUI.EnableVoice(false);
                    participant.SetIsMuteForAll(false, null);//��Ҫע���������������첽��,����������ǰ�����˳���lobby,���ᵼ��һ��Vivox����.
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
        if (m_channelSession == null || m_vivoxId == null)//��֤�Ƿ��ʼ��,���ڶԱ��غ�Զ�̿ͻ���,SetId��OnChannelJoined�������ڲ�ͬ��ʱ��.
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