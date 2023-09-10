﻿using System;
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
            m_task = Task.Run(() => { WorkerProcess().Wait(); });
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
            while (true)
            {
               var service =  SunNet.Instance.PopGlobalQueue();
                if (service == null)
                {
                    //SunNet.Instance.Log.Info(string.Format("worker id:{0} wait", m_id));
                    SunNet.Instance.WorkerWait();
                }
                else
                {
                    //SunNet.Instance.Log.Info(string.Format("worker id:{0} process {1}", m_id, service.GetType().Name));
                    await service.ProcessMsgs(m_eachNum);
                    CheckAndPutGlobal(service);
                }
            }
        }
    }
}
