using UnityEngine;

public class EmoteButtonUI : UIPanelBase
{
    [SerializeField]
    EnumEmoteType m_emoteType;

    public void SetPlayerEmote()
    {
        Manager.SetLocalPlayerEmote(m_emoteType);
    }
}
