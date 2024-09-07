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
            m_CountDownText.SetText("等待所有玩家...");
        else
            m_CountDownText.SetText($"开始于: {time:0}");//注意":0"格式是四舍五入的,而不是截断的.
    }
}
