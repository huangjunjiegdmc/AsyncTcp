using AsyncTcpServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpServerTest.TcpServer
{
    public class MyLogger : ILogger
    {
        /// <summary>
        /// 日志
        /// </summary>
        static log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Log(object message)
        {
            log.Debug(message);
        }

        public void Log(object message, Exception ex)
        {
            log.Debug(message, ex);
        }
    }
}
