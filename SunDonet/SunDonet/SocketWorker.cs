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
        SemaphoreSlim m_tasksLock;
        public LinkedList<Task<Conn>> m_tasks = new LinkedList<Task<Conn>>();

        private object m_connsLock = new object();
        public List<Conn> m_connList = new List<Conn>();

        ManualResetEvent m_notnullEvent;

        public void Init()
        {
            m_notnullEvent = new ManualResetEvent(false);
            m_tasksLock = new SemaphoreSlim(1);
        }

        public void Start()
        {
            Task.Run(() => { WorkerProcess().Wait(); });
        }

        public void AddEvent(Conn conn)
        {
            conn.ConstructTask();
            conn.m_task.Start();

            m_tasksLock.Wait();
            {
                m_tasks.AddLast(conn.m_task);
                if (m_tasks.Count != 0)
                    m_notnullEvent.Set();
            }
            m_tasksLock.Release();

            lock (m_connsLock)
            {
                m_connList.Add(conn);
            }

        }

        public void RemoveEvent(Conn conn)
        {
            //Console.WriteLine(string.Format("SocketWorker:RemoveEvent {0}", conn.m_socket.RemoteEndPoint.ToString()));
            lock (m_connList)
            {
                m_connList.Remove(conn);
            }

            if (conn.m_task != null)
            {
                m_tasksLock.Wait();
                {
                    m_tasks.Remove(conn.m_task);
                    if (m_tasks.Count == 0)
                        m_notnullEvent.Reset();
                }
                m_tasksLock.Release();
            }
        }

        public async Task WorkerProcess()
        {
            while (true)
            {
                try
                {
                    Task<Conn> completedTask = null;
                    if (m_tasks.Count <= 0)
                    {
                        //wait
                        m_notnullEvent.WaitOne();
                    }
                    m_tasksLock.Wait();
                    {
                        //Console.WriteLine("Task.WhenAny(m_tasks)");
                        completedTask = await Task.WhenAny(m_tasks);
                    }
                    m_tasksLock.Release();

                    if (completedTask != null)
                    {
                        if (completedTask.Status == TaskStatus.RanToCompletion)
                        {
                            var completedConn = completedTask.Result;
                            var res = ProcessEvent(completedConn);
                            if (res)
                            {
                                completedConn.ConstructTask();
                                completedConn.m_task.Start();

                                m_tasksLock.Wait();
                                {
                                    m_tasks.Remove(completedTask);
                                    m_tasks.AddLast(completedConn.m_task);
                                }
                                m_tasksLock.Release();
                            }
                            else
                            {
                                m_tasksLock.Wait();
                                {
                                    m_tasks.Remove(completedTask);
                                }
                                m_tasksLock.Release();
                            }
                            
                        }
                        else
                        {
                            Console.WriteLine(string.Format("task failed! {0}", completedTask.Exception.ToString()));

                            m_tasksLock.Wait();
                            {
                                m_tasks.Remove(completedTask);
                            }
                            m_tasksLock.Release();
                        }

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

            }
        }

        private bool ProcessEvent(Conn conn)
        {
            if (conn.m_socketType == SocketType.Listen)
            {
                var s = conn.m_event.AcceptSocket;
                Console.WriteLine(String.Format("客户 {0} 连入", s.RemoteEndPoint.ToString()));
                var clientConn = SunNet.Instance.AddConn(s, SocketType.Normal,conn.m_serviceId);
                this.AddEvent(clientConn);
                //向服务发送onAccept消息
                SunNet.Instance.Send(conn.m_serviceId, new SocketAcceptMsg() { m_type = MsgBase.MsgType.Socket_Accept, m_listen = conn.m_socket, m_client = s });
            }
            else if (conn.m_socketType == SocketType.Normal)
            {
                if (conn.m_event.SocketError == SocketError.Success)
                {
                    if (conn.m_event.BytesTransferred > 0)
                    {
                        if (conn.m_socket.Available == 0)
                        {
                            ClientBuffer buffer = ClientBuffer.GetBuffer(conn.m_event.BytesTransferred);
                            Array.Copy(conn.m_event.Buffer, conn.m_event.Offset, buffer.m_buffer, 0, conn.m_event.BytesTransferred);
                            buffer.m_dataLen = conn.m_event.BytesTransferred;
                            //Console.WriteLine(String.Format("客户 {0} 写入{1}", conn.m_socket.RemoteEndPoint.ToString(), System.Text.Encoding.UTF8.GetString(data)));
                            //向服务发送消息
                            SunNet.Instance.Send(conn.m_serviceId, new SocketDataMsg() { m_type = MsgBase.MsgType.Socket_Data,m_socket = conn.m_socket, m_buff = buffer});
                        }
                    }
                    else
                    {
                        //客户端主动断开连接
                        Console.WriteLine(String.Format("客户 {0} disconnected", conn.m_socket.RemoteEndPoint.ToString()));
                        SunNet.Instance.CloseConn(conn.m_socket);
                        //向服务发送消息
                        SunNet.Instance.Send(conn.m_serviceId, new SocketDisconnectMsg() { m_type = MsgBase.MsgType.Socket_Disconnect, m_client = conn.m_socket });

                        return false;
                    }
                }
                else
                {
                    Console.WriteLine(String.Format("客户 {0} Error:{1}", conn.m_socket.RemoteEndPoint.ToString(), conn.m_event.SocketError));
                    return false;
                }

            }
            return true;
        }
    }


}
