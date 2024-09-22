using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行倒计时(进入游戏中状态).虽然倒计时的开始是通过Relay同步的,但倒计时本身是本地处理的,因为时间没有必要非常精确.
/// Q:倒计时的开始是通过Relay同步的?但是本地是在倒计时结束的时候开始填充Relay连接数据的啊!难道这里指的是LobbyService底层用的Relay同步的,而非游戏内的Relay连接?
/// 是否可以跟踪断点,查看LobbyService.Instance.UpdatePlayerAsync底层发送实现,是通过什么Relay、什么地址连接的吗?
/// </summary>
[RequireComponent(typeof(CountDownUI))]
public class CountDown : MonoBehaviour
{
    CallbackValue<float> TimeLeft = new CallbackValue<float>();
    CountDownUI m_ui;
    const int k_countDownTime = 4;

    void Start()
    {
        m_ui = GetComponent<CountDownUI>();
        TimeLeft.onChanged += m_ui.OnTimeChanged;
        TimeLeft.Value = -1;
    }

    public void StartCountDown()
    {
        TimeLeft.Value = k_countDownTime;
    }

    public void CancelCountDown()
    {
        TimeLeft.Value = -1;
    }

    private void Update()
    {
        if (TimeLeft.Value < 0)
            return;

        TimeLeft.Value -= Time.deltaTime;
        if (TimeLeft.Value < 0)
            GameManager.Instance.FinishedCountDown();
    }
}
