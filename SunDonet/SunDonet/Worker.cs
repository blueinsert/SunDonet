using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    public class Worker
    {
        public int m_id;
        public int m_eachNum;

        public Task m_task;

        public void StartWorkerTask()
        {
            m_task = Task.Run(() => { WorkerProcess(); });
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
                    service.m_isInGlobal = false;
                }
            }
        }

        public void WorkerProcess()
        {
            while (true)
            {
               var service =  SunNet.Instance.PopGlobalQueue();
                if (service == null)
                {
                    SunNet.Instance.WorkerWait();
                }
                else
                {
                    service.ProcessMsgs(m_eachNum);
                    CheckAndPutGlobal(service);
                }
            }
        }
    }
}
