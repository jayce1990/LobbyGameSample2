using System;
using System.Threading.Tasks;
using UnityEngine;
/// <summary>
/// 管理调用服务器API,在一段时间里可以有几次.
/// 添加了一个pingBuffer,用于多倒计时一点时间(避免因为ping网络延迟的抖动,导致调用服务器API时过频失败).
/// TaskQueued用于让调用次数里的最后一个调用,去排队,等待冷却时间结束后,才发送.若true,则表明一段时间内的无可用次数,上层调用则直接返回.
/// 一段冷却时间内只允许一次调用的,则上层调用的地方可以直接用IsCollingDown来判断是否可以调用,为true直接上层返回.
/// 而一段冷却时间内可以多次调用的,IsCollidingDown在一段时间内一直是true,但可能可调用次数还有.所以可以通过TaskQueued判断是否最后一次调用已用掉,若true则上层直接return.
/// </summary>
public class ServiceRateLimiter
{
    public Action<bool> onCooldownChange;

    public readonly int coolDownMS;//一段时间
    readonly int m_ServiceCallTimes;//调用次数
    int m_TaskCounter;//当前调用次数
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

    //(如果依然获取到频率限制错误,尝试增加pingBuffer,使其多冷却一会包含网络延迟)
    public ServiceRateLimiter(int callTimes, float coolDown, int pingBuffer = 100)
    {
        m_ServiceCallTimes = callTimes;
        m_TaskCounter = m_ServiceCallTimes;
        coolDownMS = Mathf.CeilToInt(coolDown * 1000) + pingBuffer;
    }

    //并行冷却异步
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
            ParallelCooldownAsync();//并行执行,下面不等待它,继续执行.
#pragma warning restore 4014
        }

        m_TaskCounter--;

        if (m_TaskCounter > 0)//还有可调用次数
        {
            return;
        }

        if (!TaskQueued)
        {
            TaskQueued = true;//(一段时间内的)最后一次调用.
            //等待冷却完成才开始调用该调用.(那这样一段时间里的最后一次调用,不就跑到下一段时间里才调用了吗?会不会出现6秒中调用1次,那么调用第一次的时候需要等6秒?TODO:待测试.)
            while (m_CoolingDown)
            {
                await Task.Delay(10);
            }
        }
        else
        {
            Debug.LogError("等待最后一次调用中,上层应该判断TaskQueued=true时,不再调用该QueueUntilCooldown(),直接return.");
        }
    }
}