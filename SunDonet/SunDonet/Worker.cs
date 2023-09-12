using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace SunDonet
{
    public enum WorkerState
    {
        None,
        Runing,
        Waiting,
        //Stopping,
        Stopped,
    }

    public class Worker
    {
        public int m_id;
        public int m_eachNum;

        public WorkerState State { get { return m_state; } }

        private Task m_task;
        private CancellationTokenSource m_cancelSource;
        private CancellationToken m_cancelToken;
        private WorkerState m_state = WorkerState.None;

        public void StartWorkerTask()
        {
            m_cancelSource = new CancellationTokenSource();
            m_cancelToken = m_cancelSource.Token;
            m_task = Task.Run(() => { WorkerProcess().Wait(); }, m_cancelToken);
            m_state = WorkerState.Runing;
        }

        public void Stop()
        {
            //m_state = WorkerState.Stopping;
            m_cancelSource.Cancel();
        }

        private void CheckAndPutGlobal(ServiceBase service)
        {
            lock (service.m_msgQueueLock)
            {
                if (service.m_msgQueue.Count != 0)
                {
                    //重新放回全局队列
                    SunNet.Instance.PushGlobalQueue(service);
                }
                else
                {
                    lock (service.m_isInGlobalLock) {
                        service.m_isInGlobal = false;
                    }     
                }
            }
        }

        public async Task WorkerProcess()
        {
            while (!m_cancelToken.IsCancellationRequested)
            {
               var service =  SunNet.Instance.PopGlobalQueue();
                if (service == null)
                {
                    //SunNet.Instance.Log.Info(string.Format("worker id:{0} wait", m_id));
                    m_state = WorkerState.Waiting;
                    SunNet.Instance.WorkerWait();
                }
                else
                {
                    //SunNet.Instance.Log.Info(string.Format("worker id:{0} process {1}", m_id, service.GetType().Name));
                    m_state = WorkerState.Runing;
                    await service.ProcessMsgs(m_eachNum);
                    CheckAndPutGlobal(service);
                }
            }
            Debug.Log("Worker id:{0} stoped", m_id);
            m_state = WorkerState.Stopped;
        }
    }
}
