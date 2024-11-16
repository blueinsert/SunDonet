namespace kcp2k
{
    // channel type and header for raw messages
    public enum KcpChannel : byte
    {
        // don't react on 0x00. might help to filter out random noise.
        /// <summary>
        /// 使用kcp作为中间层进行发包收包
        /// </summary>
        Reliable   = 1,
        /// <summary>
        /// 不适用kcp
        /// </summary>
        Unreliable = 2
    }
}