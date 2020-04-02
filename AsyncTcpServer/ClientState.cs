using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpServer
{
    /// <summary>
    /// 客户端数据接收状态
    /// </summary>
    public class ClientState
    {
        /// <summary>
        /// 与客户端的会话信息
        /// </summary>
        public object ServerSession { get;  set; }

        /// <summary>
        /// 数据缓冲区
        /// </summary>
        public byte[] Buffer { get; private set; }

        /// <summary>
        /// 客户端数据接收状态
        /// </summary>
        /// <param name="session">与客户端的会话信息</param>
        /// <param name="buffer">数据缓冲区</param>
        public ClientState(object session, byte[] buffer)
        {
            ServerSession = session;
            Buffer = buffer;
        }
    }
}
