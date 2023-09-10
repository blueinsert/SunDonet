using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SunDonet
{
    /// <summary>
    /// timer节点
    /// </summary>
    public class ThreadTimerNode
    {
        public ThreadTimerNode(Int64 id, TimerCallback callback, object state, int dueTime, int period)
        {
            m_id = id;
            m_callback = callback;
            m_state = state;
            m_dueTime = dueTime;
            m_period = period;
            // 记录调用时间
            m_nextCallTime = DateTime.Now.AddMilliseconds(dueTime);
        }

        /// <summary>
        /// 到时调用
        /// </summary>
        public void MakeTimerCall()
        {
            m_callback(m_state);
            // 记录调用时间
            m_nextCallTime = DateTime.Now.AddMilliseconds(m_period);
        }

        /// <summary>
        /// 唯一ID
        /// </summary>
        public Int64 m_id;
        /// <summary>
        /// 下次调用时间
        /// </summary>
        public DateTime m_nextCallTime;
        /// <summary>
        /// 回调函数
        /// </summary>
        public TimerCallback m_callback;
        /// <summary>
        /// 回调参数
        /// </summary>
        public object m_state;
        /// <summary>
        /// 初始执行等待时间
        /// </summary>
        public int m_dueTime;
        /// <summary>
        /// 执行周期
        /// </summary>
        public int m_period;
    }

    public class ThreadTimer
    {
        /// <summary>
        /// 状态定义
        /// </summary>
        public enum State
        {
            None = 0,
            Runing,
            Pauseing,
            Paused,
            Stoping,
            Stoped,
        }

        /// <summary>
        /// 上一帧睡眠时间
        /// </summary>
        private int m_lastSleepTime;

        /// <summary>
        /// 1次tick固定时间 单位毫秒
        /// </summary>
        private readonly int m_minTickDeltaTime;

        /// <summary>
        /// 线程任务
        /// </summary>
        protected Task m_task = null;

        /// <summary>
        /// 任务取消标记控制类对象
        /// </summary>
        protected CancellationTokenSource m_cancelSource = null;

        /// <summary>
        /// 任务取消标记结构对象
        /// </summary>
        protected CancellationToken m_cancelToken;

        private Int64 m_timerIdGenerator = 0;
        protected ConcurrentDictionary<Int64, ThreadTimerNode> m_timerDic = new ConcurrentDictionary<long, ThreadTimerNode>();


        public ThreadTimer(int minTickDeltaTime = 500)
        {
            m_minTickDeltaTime = minTickDeltaTime;
        }

        public void StartTask(TaskCreationOptions option = TaskCreationOptions.LongRunning)
        {
            if (m_task != null)
                return;
            m_cancelSource = new CancellationTokenSource();
            m_cancelToken = m_cancelSource.Token;
            m_task = new Task(ThreadProc, m_cancelToken, option);
            m_task.Start();
        }

        public Int64 AddTimer(TimerCallback callback, object state, int dueTime, int period)
        {
            // 参数检查
            System.Diagnostics.Debug.Assert(callback != null);
            System.Diagnostics.Debug.Assert(dueTime >= 0);
            System.Diagnostics.Debug.Assert(period >= 0);

            // 获取该循环Timer唯一ID
            Int64 timerId = Interlocked.Increment(ref m_timerIdGenerator);

            // 添加timer
            var node = new ThreadTimerNode(timerId, callback, state, dueTime, period);
            m_timerDic.TryAdd(timerId, node);

            return timerId;
        }


        /// <summary>
        /// 移除timer
        /// </summary>
        /// <param name="timerId"></param>
        public bool RemoveTimer(Int64 timerId)
        {
            return m_timerDic.TryRemove(timerId, out _);
        }

        protected void ThreadProc()
        {
            Int32 expectedSleepTime = 0; //期望下次睡眠时间
            int deltaMilliseconds = 0;
            DateTime lastTickTime = DateTime.Now;

            try
            {
                while (!m_cancelToken.IsCancellationRequested)
                {
                    Task.Delay(expectedSleepTime).Wait();

                    try
                    {
                        // 调用OnTick
                        deltaMilliseconds = (int)((DateTime.Now - lastTickTime).TotalMilliseconds);
    
                        lastTickTime = DateTime.Now;
                        expectedSleepTime = OnTick(deltaMilliseconds);
                    }
                    catch (Exception e)
                    {
                        SunNet.Instance.Log.Info(string.Format("ThreadTimer::ThreadProc exception in OnTick e ={0}", e.ToString()));
                        // 发生错误后，设置正常的sleep时间，避免onTick被无限次调用
                        expectedSleepTime = m_minTickDeltaTime;
                    }
                }

            }
            catch (Exception e)
            {
                SunNet.Instance.Log.Info(string.Format("ThreadTimer::ThreadProc exception in while e ={0}", e.ToString()));
            }
        }

        protected virtual int OnTick(int deltaMilliseconds)
        {
            var tickStartTime = DateTime.Now;
            foreach (var node in m_timerDic)
            {
                var nextCallTime = node.Value.m_nextCallTime;
                if (nextCallTime <= tickStartTime)
                {
                    node.Value.MakeTimerCall();
                }
            }

            if (deltaMilliseconds <= m_minTickDeltaTime + m_lastSleepTime)
            {
                m_lastSleepTime = m_lastSleepTime + (m_minTickDeltaTime  - deltaMilliseconds);
            }
            else//如果会使计算结果为负数
            {
                m_lastSleepTime = 1;
            }

            return m_lastSleepTime;
        }
    }
}
