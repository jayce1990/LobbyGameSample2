using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���е���ʱ(������Ϸ��״̬).��Ȼ����ʱ�Ŀ�ʼ��ͨ��Relayͬ����,������ʱ�����Ǳ��ش����,��Ϊʱ��û�б�Ҫ�ǳ���ȷ.
/// Q:����ʱ�Ŀ�ʼ��ͨ��Relayͬ����?���Ǳ������ڵ���ʱ������ʱ��ʼ���Relay�������ݵİ�!�ѵ�����ָ����LobbyService�ײ��õ�Relayͬ����,������Ϸ�ڵ�Relay����?
/// �Ƿ���Ը��ٶϵ�,�鿴LobbyService.Instance.UpdatePlayerAsync�ײ㷢��ʵ��,��ͨ��ʲôRelay��ʲô��ַ���ӵ���?
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
