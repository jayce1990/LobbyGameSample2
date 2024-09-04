using UnityEngine;

[SerializeField]
public class LobbyUserAudio
{
    public string ID { get; private set; }

    public bool HasVoice { get; set; }

    public bool Muted { get; set; }

    private float m_userVolume;

    public float UserVolume
    {
        get => m_userVolume;
        set => m_userVolume = Mathf.Clamp01(value);
    }

    public LobbyUserAudio(string userID)
    {
        ID = userID;
        HasVoice = false;
        Muted = false;
        UserVolume = 50 / 70f;//从范围中的中性音量开始
    }
}
