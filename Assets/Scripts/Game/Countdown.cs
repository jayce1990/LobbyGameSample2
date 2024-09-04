using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���е���ʱ(������Ϸ��״̬).��Ȼ����ʱ�Ŀ�ʼ��ͨ��Relayͬ����,������ʱ�����Ǳ��ش����,��Ϊʱ��û�б�Ҫ�ǳ���ȷ.
/// Q:����ʱ�Ŀ�ʼ��ͨ��Relayͬ����?���Ǳ������ڵ���ʱ������ʱ��ʼ���Relay�������ݵİ�!�ѵ�����ָ����LobbyService�ײ��õ�Relayͬ����,������Ϸ�ڵ�Relay����?
/// �Ƿ���Ը��ٶϵ�,�鿴LobbyService.Instance.UpdatePlayerAsync�ײ㷢��ʵ��,��ͨ��ʲôRelay��ʲô��ַ���ӵ���?
/// </summary>
//[RequireComponent(typeof(CountdownUI))]
public class Countdown : MonoBehaviour
{
    CallbackValue<float> TimeLeft = new CallbackValue<float>();
    //CountdownUI m_ui;
    const int k_countdownTime = 4;

    void Start()
    {
        //m_ui = GetComponent<CountdownUI>();
        //TimeLeft.onChanged += m_ui.OnTimeChanged;
        TimeLeft.Value = k_countdownTime;
    }

    public void StartCountdown()
    {
        TimeLeft.Value = k_countdownTime;
    }

    public void CancelCountdown()
    {
        TimeLeft.Value = -1;
    }

    private void Update()
    {
        if (TimeLeft.Value < 0)
            return;

        TimeLeft.Value -= Time.deltaTime;
        //if (TimeLeft.Value < 0)
            //GameManager.Instance.FinishCountdown();
    }
}
