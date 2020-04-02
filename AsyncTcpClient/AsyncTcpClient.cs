using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpClient
{
    /// <summary>
    /// 异步TCP客户端
    /// </summary>
    public class AsyncTcpClient
    {
        #region 常量

        /// <summary>
        /// 定义缓冲区
        /// </summary>
        public const int BUFFER_SIZE = 1024 * 1024;

        #endregion


        #region 变量



        #endregion


        #region 属性

        /// <summary>
        /// 日志接口
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// 数据接收过滤器
        /// </summary>
        public IReceiveDataFilter ReceiveDataFilter { get; set; }

        /// <summary>
        /// 通信使用的字符编码
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// 是否启用KeepAlive
        /// </summary>
        public bool EnableKeepAlive { get; set; }

        #endregion
    }
}
