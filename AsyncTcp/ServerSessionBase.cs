using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcp
{
    /// <summary>
    /// 会话信息基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ServerSessionBase<T> where T : ServerSessionBase<T>, new()
    {
        /// <summary>
        /// 会话ID
        /// </summary>
        public string SessionID { get; set; }

        /// <summary>
        /// 连接时间
        /// </summary>
        public DateTime ConnectTime { get; set; }

        /// <summary>
        /// 该会话关联的TcpClient
        /// </summary>
        public TcpClient TcpClient { get; set; }

        /// <summary>
        /// 会话的客户端信息
        /// </summary>
        public IPEndPoint IpEndPoint { get; set; }

        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected => TcpClient.Connected;

        /// <summary>
        /// 发送信息
        /// </summary>
        /// <param name="message"></param>
        public abstract void SendMessage(string message);

        /// <summary>
        /// 该会话所属的TCP监听服务
        /// </summary>
        public AsyncTcpServer<T> Server { get; set; }

    }
}
