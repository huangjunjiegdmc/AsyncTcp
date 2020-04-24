using AsyncTcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpServerTest.TcpServer
{
    /// <summary>
    /// 服务端与客户端的会话
    /// </summary>
    public class MyServerSession : ServerSessionBase<MyServerSession>
    {
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        public override void SendMessage(string message)
        {
            try
            {
                byte[] sendMessage = this.Server.ReceiveDataFilter.PackageMessage(message);
                TcpClient.GetStream().Write(sendMessage, 0, sendMessage.Length);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
