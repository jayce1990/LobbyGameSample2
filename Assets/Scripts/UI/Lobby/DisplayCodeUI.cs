using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// ����Lobby����Relay��Code����,�Դ�����Ա��ʾ��ǰ��Code.
/// </summary>
public class DisplayCodeUI : UIPanelBase
{
    public enum EnumCodeType { Lobby = 0, Relay = 1 };
    [SerializeField]
    EnumCodeType m_codeType;

    [SerializeField]
    TMP_InputField m_outputText;

    public override void Start()
    {
        base.Start();

        if (m_codeType == EnumCodeType.Lobby)
            Manager.LocalLobby.LobbyCode.onChanged += LobbyCodeChanged;
        if (m_codeType == EnumCodeType.Relay)
            Manager.LocalLobby.RelayCode.onChanged += LobbyCodeChanged;
    }

    void LobbyCodeChanged(string newCode)
    {
        if (!string.IsNullOrEmpty(newCode))
        {
            m_outputText.text = newCode;
            Show();
        }
        else
        {
            Hide();
        }
    }
}
