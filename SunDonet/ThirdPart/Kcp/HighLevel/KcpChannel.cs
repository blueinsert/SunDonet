namespace kcp2k
{
    // channel type and header for raw messages
    public enum KcpChannel : byte
    {
        // don't react on 0x00. might help to filter out random noise.
        /// <summary>
        /// ʹ��kcp��Ϊ�м����з����հ�
        /// </summary>
        Reliable   = 1,
        /// <summary>
        /// ������kcp
        /// </summary>
        Unreliable = 2
    }
}