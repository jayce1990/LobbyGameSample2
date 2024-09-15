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
    [Tooltip("这是为其他玩家显示的,可用于静音他们.")]
    GameObject m_muteIcon;
    [SerializeField]
    [Tooltip("这是为本地玩家显示的,以便清楚的表明他正在静音中")]
    GameObject m_micMuteIcon;

    public bool IsLocalPlayer { private get; set; }

    /// <param name="shouldRestUi">
    /// 当玩家被添加大厅,我们想重置UI到默认值。
    /// 但我们不想假如用户已经在大厅中了,玩家可能已修改了的值,被重置成默认值。
    /// </param>
    public void EnableVoice(bool shouldRestUi = false)
    {
        if (shouldRestUi)//重置默认值
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
