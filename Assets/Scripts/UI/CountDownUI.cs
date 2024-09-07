using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CountDownUI : UIPanelBase
{
    [SerializeField]
    TMP_Text m_CountDownText;

    public void OnTimeChanged(float time)
    {
        if (time <= 0)
            m_CountDownText.SetText("�ȴ��������...");
        else
            m_CountDownText.SetText($"��ʼ��: {time:0}");//ע��":0"��ʽ�����������,�����ǽضϵ�.
    }
}
