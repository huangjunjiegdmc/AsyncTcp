using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcp
{
    /// <summary>
    /// 数据接收过滤器接口
    /// </summary>
    public interface IReceiveDataFilter
    {
        /// <summary>
        /// 下一个过滤器：当一个消息未接收完整，继续时使用上一个过滤器接收的缓存消息，再接收后续数据
        /// </summary>
        IReceiveDataFilter NextReceiveDataFilter { get; }

        /// <summary>
        /// 查找符合条件的消息，有则返回true，否则返回false
        /// </summary>
        /// <param name="nextOne">是否开始接收下一个消息，否，则表示继续接收上一消息</param>
        /// <param name="readBuffer">已接收到的数据缓冲数组</param>
        /// <param name="rest">有符合条件的消息时，剩下未处理的数据长度</param>
        /// <returns>接收到的符合条件的消息数组</returns>
        byte[] Filter(List<byte> readBuffer, out int rest);

        /// <summary>
        /// 重置过滤器
        /// </summary>
        void Reset();

        /// <summary>
        /// 删除消息的截取标志信息，如开始结束标志、消息长度标志、校验码等
        /// </summary>
        /// <param name="byteReceived">完整消息</param>
        /// <returns>删除消息的截取标志信息后的纯消息内容</returns>
        byte[] RemoveMessageFilterFlag(byte[] byteReceived);

        /// <summary>
        /// 打包要发出去的消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <returns>打包后的消息字节数组</returns>
        byte[] PackageMessage(string message);

        /// <summary>
        /// 通信使用的字符编码
        /// </summary>
        Encoding Encoding { get; set; }

        /// <summary>
        /// 日志接口
        /// </summary>
        ILogger Logger { get; set; }
    }
}
