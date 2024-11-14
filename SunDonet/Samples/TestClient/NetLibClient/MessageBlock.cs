
using System;
using System.IO;
using System.Diagnostics;

namespace NetLibClient
{
    public class MessageBlock
    {
        public MessageBlock(Int32 maxLength)
        {
            m_buffer = new byte[maxLength];
            m_rdPtr = 0;
            m_wrPtr = 0;
        }

        /// <summary>
        /// Write the specified data to cache.
        /// </summary>
        /// <param name="dataBlock">Data block to be writed.</param>
        /// <param name="dataLen">Data block length.</param>
        /// <returns>The length has writed to cahche.</returns>
        public Int32 Write(byte[] dataBlock,Int32 offset, Int32 dataLen)
        {
            // Shit
            Debug.Assert(dataBlock != null);
            Debug.Assert(dataLen > 0);
            Debug.Assert(dataBlock.Length >= dataLen);

            // 空间不够重新分配
            while (dataLen > Space)
            {
                Array.Resize(ref m_buffer, m_buffer.Length * 2);
            }

            // Copy to cache
            Array.Copy(dataBlock, offset, m_buffer, m_wrPtr, dataLen);
            m_wrPtr += dataLen;

            // Return copied length
            return dataLen;
        }

        /// <summary>
        /// Read 4 bytes as int32 value.
        /// </summary>
        /// <returns>The int32 value.</returns>
        public Int32 ReadInt32()
        {
            //Debug.Assert(Length >= sizeof(Int32));

            // Convert and move ahead read pointer.
            Int32 rValue = BitConverter.ToInt32(m_buffer, m_rdPtr);
            m_rdPtr += sizeof(Int32);

            return rValue;
        }

        /// <summary>
        /// Read 2 bytes as UInt16 value.
        /// </summary>
        /// <returns>The int32 value.</returns>
        public UInt16 ReadUInt16()
        {
            UInt16 rValue = BitConverter.ToUInt16(m_buffer, m_rdPtr);
            m_rdPtr += sizeof(UInt16);

            return rValue;
        }

        /// <summary>
        /// Peek 4 bytes as int32 value.
        /// </summary>
        /// <returns>The int32 value.</returns>
        public Int32 PeekInt32()
        {
            Debug.Assert(Length >= sizeof(Int32));

            return
                BitConverter.ToInt32(m_buffer, m_rdPtr);
        }

        /// <summary>
        /// Move adhead read pointer by given offset value.
        /// </summary>
        /// <returns>New read pointer.</returns>
        /// <param name="ahOffset">Move ahead offset.</param>
        public Int32 ReadPtr(Int32 ahOffset)
        {
            m_rdPtr += ahOffset;
            Debug.Assert(m_rdPtr >= 0 && m_rdPtr <= m_wrPtr);

            return m_rdPtr;
        }

        /// <summary>
        /// Get memory stream to contain specific length data an move read ptr ahead.
        /// </summary>
        /// <returns>The memory stream hold specific data.</returns>
        /// <param name="dataLen">Data length.</param>
        public MemoryStream GetReadStream(Int32 dataLen)
        {
            Debug.Assert(dataLen <= Length);

            MemoryStream newStream = new MemoryStream(m_buffer, m_rdPtr, dataLen, false);
            m_rdPtr += dataLen;
            return newStream;
        }

        public void ReadSkip(Int32 datalen)
        {
            m_rdPtr += datalen;
        }

        /// <summary>
        /// Normalizes data to align with the base.
        /// </summary>
        public void Crunch()
        {
            // Go away
            if (m_rdPtr == 0)
                return;

            // Not neccessary
            //if (m_rdPtr < m_buffer.Length / 5)
            //    return;

            // Go simple way
            if (Length == 0)
            {
                m_rdPtr = 0;
                m_wrPtr = 0;
            }
            // Ops, copy, copy
            else
            {
                Int32 length = Length;
                //byte[] holdBuf = new byte[length];
                //Array.Copy(_cache, _rdPtr, holdBuf, 0, length);
                //Array.Copy(holdBuf, _cache, length);
                Array.Copy(m_buffer, m_rdPtr, m_buffer, 0, length);
                m_rdPtr = 0;
                m_wrPtr = length;
            }
        }

        /// <summary>
        /// Get data length.
        /// </summary>
        /// <value>Data length.</value>
        public Int32 Length
        {
            get
            {
                return (m_wrPtr - m_rdPtr);
            }
        }

        /// <summary>
        /// Get current space for write data.
        /// </summary>
        /// <value>The cache space.</value>
        public Int32 Space
        {
            get
            {
                return (m_buffer.Length - m_wrPtr);
            }
        }

        public byte[] m_buffer;
        private Int32 m_rdPtr;
        private Int32 m_wrPtr;
    }
}


