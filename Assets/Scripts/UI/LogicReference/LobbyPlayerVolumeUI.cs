using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerVolumeUI : MonoBehaviour
{
    [SerializeField]
    Slider m_volumeSlider;
    [SerializeField]
    Toggle m_muteToggle;

    [SerializeField]
    UIPanelBase m_volumeSliderContainer;
    [SerializeField]
    UIPanelBase m_muteToggleContainer;

    [SerializeField]
    [Tooltip("����Ϊ���������ʾ��,�����ھ�������.")]
    GameObject m_muteIcon;
    [SerializeField]
    [Tooltip("����Ϊ���������ʾ��,�Ա�����ı��������ھ�����")]
    GameObject m_micMuteIcon;

    public bool IsLocalPlayer { private get; set; }

    /// <param name="shouldRestUi">
    /// ����ұ���Ӵ���,����������UI��Ĭ��ֵ��
    /// �����ǲ�������û��Ѿ��ڴ�������,��ҿ������޸��˵�ֵ,�����ó�Ĭ��ֵ��
    /// </param>
    public void EnableVoice(bool shouldRestUi = false)
    {
        if (shouldRestUi)//����Ĭ��ֵ
        {
            m_volumeSlider.SetValueWithoutNotify(VivoxPlayerHandler.NormalizedVolumeDefault);
            m_muteToggle.SetIsOnWithoutNotify(false);
        }

        if (IsLocalPlayer)
        {
            m_volumeSliderContainer.Hide(0);
            m_muteToggleContainer.Show();
            m_muteIcon.SetActive(false);
            m_micMuteIcon.SetActive(true);
        }
        else
        {
            m_volumeSliderContainer.Show();
            m_muteToggleContainer.Show();
            m_muteIcon.SetActive(true);
            m_micMuteIcon.SetActive(false);
        }
    }

    public void DisableVoice(bool shouldResetUi)
    {
        if (shouldResetUi)
        {
            m_volumeSlider.value = VivoxPlayerHandler.NormalizedVolumeDefault;
            m_muteToggle.isOn = false;
        }

        m_volumeSliderContainer.Hide(0.4f);
        m_muteToggleContainer.Hide(0.4f);
        m_muteIcon.SetActive(true);
        m_micMuteIcon.SetActive(false);
    }
}
