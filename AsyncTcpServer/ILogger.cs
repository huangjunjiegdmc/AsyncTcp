using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpServer
{
    /// <summary>
    /// 日志接口
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 打印信息
        /// </summary>
        /// <param name="message"></param>
        void Log(object message);

        /// <summary>
        /// 打印信息，同时打印错误堆栈
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        void Log(object message, Exception ex);
    }
}
