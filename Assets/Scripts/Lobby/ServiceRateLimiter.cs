using System;
using System.Threading.Tasks;
using UnityEngine;
/// <summary>
/// ������÷�����API,��һ��ʱ��������м���.
/// �����һ��pingBuffer,���ڶ൹��ʱһ��ʱ��(������Ϊping�����ӳٵĶ���,���µ��÷�����APIʱ��Ƶʧ��).
/// TaskQueued�����õ��ô���������һ������,ȥ�Ŷ�,�ȴ���ȴʱ�������,�ŷ���.��true,�����һ��ʱ���ڵ��޿��ô���,�ϲ������ֱ�ӷ���.
/// һ����ȴʱ����ֻ����һ�ε��õ�,���ϲ���õĵط�����ֱ����IsCollingDown���ж��Ƿ���Ե���,Ϊtrueֱ���ϲ㷵��.
/// ��һ����ȴʱ���ڿ��Զ�ε��õ�,IsCollidingDown��һ��ʱ����һֱ��true,�����ܿɵ��ô�������.���Կ���ͨ��TaskQueued�ж��Ƿ����һ�ε������õ�,��true���ϲ�ֱ��return.
/// </summary>
public class ServiceRateLimiter
{
    public Action<bool> onCooldownChange;

    public readonly int coolDownMS;//һ��ʱ��
    readonly int m_ServiceCallTimes;//���ô���
    int m_TaskCounter;//��ǰ���ô���
    public bool TaskQueued { get; private set; } = false;
    bool m_CoolingDown = false;
    public bool IsCoolingDown
    {
        get => m_CoolingDown;
        private set
        {
            if (m_CoolingDown != value)
            {
                m_CoolingDown = value;
                onCooldownChange?.Invoke(m_CoolingDown);
            }
        }
    }

    //(�����Ȼ��ȡ��Ƶ�����ƴ���,��������pingBuffer,ʹ�����ȴһ����������ӳ�)
    public ServiceRateLimiter(int callTimes, float coolDown, int pingBuffer = 100)
    {
        m_ServiceCallTimes = callTimes;
        m_TaskCounter = m_ServiceCallTimes;
        coolDownMS = Mathf.CeilToInt(coolDown * 1000) + pingBuffer;
    }

    //������ȴ�첽
    async Task ParallelCooldownAsync()
    {
        IsCoolingDown = true;
        await Task.Delay(coolDownMS);
        IsCoolingDown = false;
        TaskQueued = false;
        m_TaskCounter = m_ServiceCallTimes;
    }

    public async Task QueueUntilCooldown()
    {
        if (!m_CoolingDown)
        {
#pragma warning disable 4014
            ParallelCooldownAsync();//����ִ��,���治�ȴ���,����ִ��.
#pragma warning restore 4014
        }

        m_TaskCounter--;

        if (m_TaskCounter > 0)//���пɵ��ô���
        {
            return;
        }

        if (!TaskQueued)
        {
            TaskQueued = true;//(һ��ʱ���ڵ�)���һ�ε���.
            //�ȴ���ȴ��ɲſ�ʼ���øõ���.(������һ��ʱ��������һ�ε���,�����ܵ���һ��ʱ����ŵ�������?�᲻�����6���е���1��,��ô���õ�һ�ε�ʱ����Ҫ��6��?TODO:������.)
            while (m_CoolingDown)
            {
                await Task.Delay(10);
            }
        }
        else
        {
            Debug.LogError("�ȴ����һ�ε�����,�ϲ�Ӧ���ж�TaskQueued=trueʱ,���ٵ��ø�QueueUntilCooldown(),ֱ��return.");
        }
    }
}