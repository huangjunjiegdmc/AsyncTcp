using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpServer
{
    /// <summary>
    /// 过滤器：开始标志-数据-结束标志
    /// </summary>
    public class BeginEndFilter : IReceiveDataFilter
    {
        /// <summary>
        /// 开始标志：垂直制表符的ASCII码为11
        /// </summary>
        private byte[] m_beginMark = new byte[] { (byte)((char)11) };
        
        /// <summary>
        /// 结束标记：文件分割符的ASCII码为28，回车符的ASCII码为13
        /// </summary>
        public byte[] m_endMark = new byte[] { (byte)((char)28), (byte)((char)13) };
        
        /// <summary>
        /// 下一个过滤器：当一个消息未接收完整，继续时使用上一个过滤器接收的缓存消息，再接收后续数据
        /// </summary>
        public IReceiveDataFilter NextReceiveDataFilter { get; set; }

        /// <summary>
        /// 已接收数据
        /// </summary>
        public List<byte> ByteReceived { get; set; }

        /// <summary>
        /// 是否已接收过数据且未接收完整
        /// </summary>
        private bool HasReceivedData { get; set; }

        /// <summary>
        /// 通信使用的字符编码
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// 日志接口
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// 重置过滤器
        /// </summary>
        public void Reset()
        {
            ByteReceived = null;
            NextReceiveDataFilter = null;
            HasReceivedData = false;
        }

        /// <summary>
        /// 查找一个完整的消息，如果不符合条件，则返回空，否则返回一个完整的消息
        /// </summary>
        /// <param name="readBuffer">标本从流中读取的缓冲数组</param>
        /// <param name="rest">查找到一个完整的消息后，剩余未处理的数组长度，查找不到完整消息时为0</param>
        /// <returns></returns>
        public byte[] Filter(List<byte> readBuffer, out int rest)
        {
            int beginIndex = 0;
            int endIndex = 0;

            byte[] receive = null;

            if (HasReceivedData)
            {
                //如果已接收过数据，则应先从0开始找结束标志
                //如果有，则返回处理
                endIndex = FindEndIndex(readBuffer, -1);//-1表示从开始0开始查找
                if (endIndex >= 0)
                {
                    ByteReceived.AddRange(readBuffer.Take(endIndex + m_endMark.Length));

                    //此时一个消息已接收完整，则返回处理
                    rest = readBuffer.Count - (endIndex + m_endMark.Length);
                    receive = ByteReceived.ToArray();
                    return receive;
                }
                else
                {
                    ByteReceived.AddRange(readBuffer);
                    NextReceiveDataFilter = this;
                    rest = 0;
                    return null;
                }
            }

            beginIndex = FindStartIndex(readBuffer);
            if (beginIndex == -1)
            {
                //如果没有开始标记，则判断是否已接收过数据
                //如果已接收过数据，再判断是否有结束标志，
                //  如果有结束标志，则将结束标志前的数据添加到已接收数据，
                //  如果没有结束标志，则将整个缓冲数组添加到已接收数据；
                //如果没有开始标记，又未接收过数据，则丢弃该段数据
                if (HasReceivedData)
                {
                    endIndex = FindEndIndex(readBuffer, beginIndex);
                    if (endIndex >= 0)
                    {
                        ByteReceived.AddRange(readBuffer.Take(endIndex + m_endMark.Length));

                        //此时一个消息已接收完整，则返回处理
                        rest = readBuffer.Count - (endIndex + m_endMark.Length);
                        receive = ByteReceived.ToArray();
                        return receive;
                    }
                    else
                    {
                        ByteReceived.AddRange(readBuffer);
                        NextReceiveDataFilter = this;
                        rest = 0;
                        return null;
                    }
                }
                else
                {
                    rest = 0;
                    Reset();

                    //如果有异常，可能是接收的数据不符合通信协议，记录日志
                    Log("Received data: " + System.Text.Encoding.Default.GetString(readBuffer.ToArray()));

                    return null;
                }
            }

            endIndex = FindEndIndex(readBuffer, beginIndex);
            if (endIndex == -1)
            {
                //如果有开始标志，没有结束标记，则将开始标志后面的数据添加到已接收数据
                ByteReceived = new List<byte>();
                ByteReceived.AddRange(readBuffer.Skip(beginIndex).ToList());
                NextReceiveDataFilter = this;
                HasReceivedData = true;
                rest = 0;
                return null;
            }
            else
            {
                //如果有开始和结束标志，则说明是一个完整的消息，则直接返回处理
                List<byte> temp = readBuffer.Take(endIndex + m_endMark.Length).ToList();//先获取结束标志前的所有数据
                temp = temp.Skip(beginIndex).ToList();//再从开始位置取后面所有数据

                ByteReceived = new List<byte>();
                ByteReceived.AddRange(temp);

                //此时一个消息已接收完整，则返回处理
                rest = readBuffer.Count - (endIndex + m_endMark.Length);
                receive = ByteReceived.ToArray();
                return receive;
            }
        }

        /// <summary>
        /// 删除消息的截取标志信息，如开始结束标志、消息长度标志、校验和标志等
        /// </summary>
        /// <param name="byteReceived">完整消息</param>
        /// <returns>删除消息的截取标志信息后的纯消息内容</returns>
        public byte[] RemoveMessageFilterFlag(byte[] byteReceived)
        {
            byte[] receive;

            receive = byteReceived.Skip(m_beginMark.Length).ToArray();
            receive = receive.Take(receive.Length - m_endMark.Length).ToArray();

            return receive;
        }

        /// <summary>
        /// 打包要发出去的消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <returns>打包后的消息字节数组</returns>
        public byte[] PackageMessage(string message)
        {
            byte[] sendMessage;
            byte[] messageSource = this.Encoding.GetBytes(message);

            List<byte> byteBuffer = new List<byte>();
            byteBuffer.AddRange(m_beginMark);
            byteBuffer.AddRange(messageSource);
            byteBuffer.AddRange(m_endMark);
            sendMessage = byteBuffer.ToArray();

            return sendMessage;
        }

        private int FindStartIndex(List<byte> readBuffer)
        {
            int beginIndex = 0;

            findAgain:
            beginIndex = readBuffer.FindIndex(p => p.Equals(m_beginMark[0]));
            if (beginIndex == -1)
            {
                return beginIndex;//第一个标志都没有，说明没有开始标志
            }

            //判断是否有完整的开始标志
            byte[] findMark = new byte[m_beginMark.Length];
            readBuffer.CopyTo(beginIndex, findMark, 0, m_beginMark.Length);
            if (!findMark.SequenceEqual(m_beginMark))
            {
                goto findAgain;
            }

            return beginIndex;
        }

        private int FindEndIndex(List<byte> readBuffer, int beginIndex)
        {
            int endIndex;

            findAgain:
            if (beginIndex == -1)
            {
                endIndex = readBuffer.FindIndex(p => p.Equals(m_endMark[0]));
            }
            else
            {
                endIndex = readBuffer.FindIndex(beginIndex + m_beginMark.Length, p => p.Equals(m_endMark[0]));
            }
            if (endIndex == -1)
            {
                return endIndex;//第一个标志都没有，说明没有结束标志
            }

            //判断是否有完整的结束标志
            byte[] findMark = new byte[m_endMark.Length];
            readBuffer.CopyTo(endIndex, findMark, 0, m_endMark.Length);
            if (!findMark.SequenceEqual(m_endMark))
            {
                goto findAgain;
            }

            return endIndex;
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message">日志消息</param>
        protected void Log(object message, Exception ex = null)
        {
            if (Logger != null)
            {
                if (ex != null)
                {
                    Logger.Log(message, ex);
                    Logger.Log(ex.StackTrace, ex);
                }
                else
                {
                    Logger.Log(message);
                }
            }
        }
    }
}
