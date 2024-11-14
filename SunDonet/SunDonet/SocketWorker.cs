using System;
using System.Collections.Generic;

using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;


namespace SunDonet
{
    public class SocketWorker
    {
        public enum SocketWorkerState
        {
            Running,
            Stoping,
            Stoped,
        }

        private SocketWorkerState m_state;
        private Task m_task;
        private CancellationTokenSource m_cancelSource;
        private CancellationToken m_cancelToken;

        private SemaphoreSlim m_tasksLock;
        /// <summary>
        /// 监听事件列表：包括一个cancelTask或若干个connTask
        /// </summary>
        private LinkedList<Task> m_tasks = new LinkedList<Task>();


        private object m_connsLock = new object();
        private List<Conn> m_connList = new List<Conn>();

        //ManualResetEvent m_notnullEvent;
        bool m_isInWaiting = false;
        CancellationTokenSource m_waitAnyCancelSource = null;
        Task m_cancelWaitAnyTask = null;

        public void Stop()
        {
            if (m_state == SocketWorkerState.Stoped)
                return;
            m_state = SocketWorkerState.Stoping;
            m_cancelSource.Cancel();
            if (m_isInWaiting)
            {
                Task.Run(() => { m_waitAnyCancelSource.Cancel(); });
            }
            m_task.Wait();
        }

        public void Initialize()
        {
            //m_notnullEvent = new ManualResetEvent(false);
            m_tasksLock = new SemaphoreSlim(1);
            m_cancelSource = new CancellationTokenSource();
            m_cancelToken = m_cancelSource.Token;
        }

        private void UnInitialize()
        {
            Debug.Log("SocketWorker:UnInitialize");
            m_connList.Clear();
            m_tasks.Clear();
            m_tasksLock.Dispose();
        }

        public void Start()
        {
            m_state = SocketWorkerState.Running;
            ConstructCancelWaitAnyTask();
            m_tasksLock.Wait();
            {
                m_tasks.AddLast(m_cancelWaitAnyTask);
            }
            m_tasksLock.Release();
            m_task = Task.Run(() => { WorkerProcess().Wait(); }, m_cancelToken);
        }

        private void ConstructCancelWaitAnyTask()
        {
            if (m_waitAnyCancelSource == null)
            {
                //m_cancelSource = new CancellationTokenSource();
            }
            m_waitAnyCancelSource = new CancellationTokenSource();
            var m_cancellationToken = m_waitAnyCancelSource.Token;
            if (m_cancellationToken.IsCancellationRequested)
            {
                m_cancelWaitAnyTask = Task.FromCanceled(m_cancellationToken);
            }
            else
            {
                //return Task.Run(() => { client.m_cancellationToken.WaitHandle.WaitOne(); });
                var tcs = new TaskCompletionSource<int>();
                m_cancellationToken.Register(() => tcs.TrySetCanceled(m_cancellationToken), useSynchronizationContext: false);
                m_cancelWaitAnyTask = tcs.Task;
            }
        }

        /// <summary>
        /// 增加监听事件
        /// 监听conn的消息传入，或者新的连接
        /// </summary>
        /// <param name="conn"></param>
        public void AddListenedConn(Conn conn)
        {
            conn.ConstructTask();
            conn.m_task.Start();
            if (m_isInWaiting)
            {
                Task.Run(() => { m_waitAnyCancelSource.Cancel(); }); 
            }
            m_tasksLock.Wait();
            {
                //Debug.Log("SocketWorker:AddEvent {0}", conn.m_socketType);
                m_tasks.AddLast(conn.m_task);
                //if (m_tasks.Count != 0)
                //    m_notnullEvent.Set();
            }
            m_tasksLock.Release();

            lock (m_connsLock)
            {
                m_connList.Add(conn);
            }

        }

        public void RemoveListenedConn(Conn conn)
        {
            //SunNet.Instance.Log.Info(string.Format("SocketWorker:RemoveEvent {0}", conn.m_socket.RemoteEndPoint.ToString()));
            lock (m_connsLock)
            {
                m_connList.Remove(conn);
            }

            if (conn.m_task != null)
            {
                if (m_isInWaiting)
                {
                    Task.Run(() => { m_waitAnyCancelSource.Cancel(); });
                }
                m_tasksLock.Wait();
                {
                    m_tasks.Remove(conn.m_task);
                    //if (m_tasks.Count == 0)
                    //    m_notnullEvent.Reset();
                }
                m_tasksLock.Release();
            }
        }

        /// <summary>
        /// 处理网络流的主循环
        /// </summary>
        /// <returns></returns>
        public async Task WorkerProcess()
        {
            while (!m_cancelToken.IsCancellationRequested)
            {
                try
                {
                    Task<Conn> completedConnTask = null;
                    Task completedTask = null;
                    //if (m_tasks.Count <= 0)
                    //{
                        //wait
                    //    m_notnullEvent.WaitOne();
                    //}
                    m_tasksLock.Wait();
                    //等待任一监听事件完成
                    {
                        m_isInWaiting = true;
                        //SunNet.Instance.Log.Info("Task.WhenAny(m_tasks)");
                        completedTask = await Task.WhenAny(m_tasks);
                        m_isInWaiting = false;
                    }
                    m_tasksLock.Release();
                    //? todo
                    if(completedTask == m_cancelWaitAnyTask)
                    {
                        ConstructCancelWaitAnyTask();
                        m_tasksLock.Wait();
                        {
                            m_tasks.Remove(completedTask);
                            m_tasks.AddLast(m_cancelWaitAnyTask);
                        }
                        m_tasksLock.Release();
                        Task.Delay(100).Wait();
                        continue;
                    }
                    completedConnTask = completedTask as Task<Conn>;
                    if (completedConnTask != null)// conn task
                    {
                        if (completedConnTask.Status == TaskStatus.RanToCompletion)
                        {
                            var state = m_state;//todo
                            var completedConn = completedConnTask.Result;
                            //state为stoping时，不处理此次监听到的事件，并且不再继续监听该socket
                            bool res = true;
                            if(state == SocketWorkerState.Running)
                                res = ProcessConnEvent(completedConn);
                            if (res)
                            {
                                if (state == SocketWorkerState.Running) {
                                    //重新构造conn task
                                    completedConn.ConstructTask();
                                    completedConn.m_task.Start();
                                }

                                m_tasksLock.Wait();
                                {
                                    m_tasks.Remove(completedConnTask);
                                    if (state == SocketWorkerState.Running)
                                    {
                                        //重新加入监听
                                        m_tasks.AddLast(completedConn.m_task);
                                    }
                                        
                                }
                                m_tasksLock.Release();
                            }
                            else
                            {
                                m_tasksLock.Wait();
                                {
                                    m_tasks.Remove(completedConnTask);
                                }
                                m_tasksLock.Release();
                            }

                        }
                        else
                        {
                            SunNet.Instance.Log.Info(string.Format("task failed! {0}", completedConnTask.Exception.ToString()));

                            m_tasksLock.Wait();
                            {
                                m_tasks.Remove(completedConnTask);
                            }
                            m_tasksLock.Release();
                        }

                    }
                }
                catch (Exception e)
                {
                    SunNet.Instance.Log.Info(e.ToString());
                }

            }

            m_state = SocketWorkerState.Stoped;
            Debug.Log("SocketWorker Stoped");
            UnInitialize(); 
        }

        private bool ProcessConnEvent(Conn conn)
        {
            if (conn.m_socketType == SocketType.Listen)
            {
                var s = conn.m_event.AcceptSocket;
                SunNet.Instance.Log.Info(String.Format("客户 {0} 连入", s.RemoteEndPoint.ToString()));
                SocketIndentifier id = new SocketIndentifier(s);
                var clientConn = SunNet.Instance.AddConn(id, s, conn.m_serviceId);
                this.AddListenedConn(clientConn);
                //向服务发送onAccept消息
                var socket = conn.m_socket;
                var socketId = SunNet.Instance.GetSocketId(socket);
                SunNet.Instance.SendInternal(conn.m_serviceId, new SocketAcceptMsg() { MessageType = MsgBase.MsgType.Socket_Accept, Listen = socketId, Client = id });
            }
            else if (conn.m_socketType == SocketType.Normal)
            {
                var socket = conn.m_socket;
                var socketId = SunNet.Instance.GetSocketId(socket);
                if (conn.m_event.SocketError == SocketError.Success)
                {
                    if (conn.m_event.BytesTransferred > 0)
                    {
                        ClientBuffer buffer = ClientBuffer.GetBuffer(conn.m_event.BytesTransferred);
                        Array.Copy(conn.m_event.Buffer, conn.m_event.Offset, buffer.m_buffer, 0, conn.m_event.BytesTransferred);
                        buffer.m_dataLen = conn.m_event.BytesTransferred;
                        //SunNet.Instance.Log.Info(String.Format("客户 {0} 写入{1}", conn.m_socket.RemoteEndPoint.ToString(), System.Text.Encoding.UTF8.GetString(buffer.m_buffer)));
                        //向服务发送消息
                        SunNet.Instance.SendInternal(conn.m_serviceId, new SocketDataMsg() { MessageType = MsgBase.MsgType.Socket_Data, SocketId = socketId, Buff = buffer });
                    }
                    else
                    {
                        //客户端主动断开连接
                        //SunNet.Instance.Log.Info(String.Format("客户 {0} disconnected", conn.m_socket.RemoteEndPoint.ToString()));
                        SunNet.Instance.CloseConn(socketId, true);
                        //向服务发送消息
                        SunNet.Instance.SendInternal(conn.m_serviceId, new SocketDisconnectMsg() { MessageType = MsgBase.MsgType.Socket_Disconnect, ClientId = socketId, Reason = "ClientActiveDisconnect" });

                        return false;
                    }
                }
                else
                {
                    //SunNet.Instance.Log.Info(String.Format("客户 {0} Error:{1}", conn.m_socket.RemoteEndPoint.ToString(), conn.m_event.SocketError));
                    SunNet.Instance.CloseConn(socketId, true);
                    //向服务发送消息
                    SunNet.Instance.SendInternal(conn.m_serviceId, new SocketDisconnectMsg() { MessageType = MsgBase.MsgType.Socket_Disconnect, ClientId = socketId, Reason = conn.m_event.SocketError.ToString() });
                    return false;
                }

            }
            return true;
        }
    }


}
