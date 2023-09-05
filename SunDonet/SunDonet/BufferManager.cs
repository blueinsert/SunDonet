using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SunDonet
{
    // This class creates a single large buffer which can be divided up
    // and assigned to SocketAsyncEventArgs objects for use with each
    // socket I/O operation.
    // This enables bufffers to be easily reused and guards against
    // fragmenting heap memory.
    //
    // The operations exposed on the BufferManager class are not thread safe.
    public class BufferManager
    {
        int m_numBytes;                 // the total number of bytes controlled by the buffer pool
        byte[] m_buffer;                // the underlying byte array maintained by the Buffer Manager
        Stack<int> m_freeIndexPool;     //
        int m_currentIndex;
        int m_bufferSize;

        public BufferManager(int totalBytes, int bufferSize)
        {
            m_numBytes = totalBytes;
            m_currentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
        }

        // Allocates buffer space used by the buffer pool
        public void InitBuffer()
        {
            // create one big large buffer and divide that
            // out to each SocketAsyncEventArg object
            m_buffer = new byte[m_numBytes];
        }

        // Assigns a buffer from the buffer pool to the
        // specified SocketAsyncEventArgs object
        //
        // <returns>true if the buffer was successfully set, else false</returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {

            if (m_freeIndexPool.Count > 0)
            {
                args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);
            }
            else
            {
                if ((m_numBytes - m_bufferSize) < m_currentIndex)
                {
                    return false;
                }
                args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
                m_currentIndex += m_bufferSize;
            }
            return true;
        }

        // Removes the buffer from a SocketAsyncEventArg object.
        // This frees the buffer back to the buffer pool
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            m_freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }

    /// <summary>
    /// 用来发送或接受buffer
    /// </summary>
    public class ClientBuffer
    {
        public ClientBuffer()
        {
            m_maxLen = m_maxLenDefault;
            m_buffer = new byte[m_maxLenDefault];
        }

        public ClientBuffer(int size)
        {
            m_maxLen = size;
            m_buffer = new byte[m_maxLen];
        }

        /// <summary>
        /// [ThreadSafe]
        /// 获取buf
        /// </summary>
        public static ClientBuffer GetBuffer(int size)
        {
            ClientBuffer buf = null;
            if (!m_outputBufferPool.TryPop(out buf))
            {
                buf = new ClientBuffer(size);
                m_lastLockTime = DateTime.Now;
            }
            else
            {
                // 如果长度不够，重新分配一下
                if (buf.m_maxLen < size)
                {
                    Array.Resize(ref buf.m_buffer, size);
                    buf.m_maxLen = size;
                }
            }

            if (m_outputBufferPool.Count == 0)
            {
                m_lastLockTime = DateTime.Now;
            }
            return buf;
        }
        /// <summary>
        /// 获取buf
        /// </summary>
        /// <returns></returns>
        public static ClientBuffer GetBuffer()
        {
            return GetBuffer(m_maxLenDefault);
        }

        /// <summary>
        /// 解锁
        /// </summary>
        /// <param name="buf"></param>
        public static void BackBuffer(ClientBuffer buf)
        {
            if (m_outputBufferPool.Count > m_maxPoolLength)
            {
                if ((DateTime.Now - m_lastLockTime).TotalMinutes > m_maxPoolFreeWaitMinutes)
                {
                    // 清理数据
                    ClientBuffer outBuf = null;
                    while (m_outputBufferPool.Count > m_maxPoolLength)
                    {
                        m_outputBufferPool.TryPop(out outBuf);
                    }
                }
            }

            buf.m_dataLen = 0;
            m_outputBufferPool.Push(buf);
        }

        /// <summary>
        /// 默认发送缓存大小
        /// </summary>
        public static int m_maxLenDefault = 8 * 1024;

        /// <summary>
        /// 最大的pool长度
        /// </summary>
        public static int m_maxPoolLength = 2000;

        /// <summary>
        /// 释放pool的等待时间
        /// </summary>
        public static int m_maxPoolFreeWaitMinutes = 5;

        /// <summary>
        /// 可设定的发送缓存大小
        /// </summary>
        private int m_maxLen;

        /// <summary>
        /// 发送缓存
        /// </summary>
        public byte[] m_buffer;

        /// <summary>
        /// 发送缓存实际数据长度
        /// </summary>
        public int m_dataLen;

        /// <summary>
        /// 缓存池中buffer的个数
        /// </summary>
        public static int PoolBuffCount { get { return m_outputBufferPool.Count; } }

        /// <summary>
        /// 缓存池
        /// </summary>
        private static ConcurrentStack<ClientBuffer> m_outputBufferPool = new ConcurrentStack<ClientBuffer>();

        private static DateTime m_lastLockTime;
    }
}
