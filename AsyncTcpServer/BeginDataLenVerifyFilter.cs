using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpServer
{
    /// <summary>
    /// 过滤器：开始标志-数据长度-数据-校验码
    /// </summary>
    public class BeginDataLenVerifyFilter : IReceiveDataFilter
    {
        /// <summary>
        /// 开始标志：垂直制表符的ASCII码为11
        /// </summary>
        private byte[] m_beginMark = new byte[] { (byte)((char)11) };

        /// <summary>
        /// 开始标志字节数
        /// </summary>
        private int m_beginMarkByteCount = 1;

        /// <summary>
        /// 数据长度，类型，包含整个消息结构长度：起始标识、消息长度、消息体、校验码构成的字节长度。
        /// </summary>
        private int m_dataLen;

        /// <summary>
        /// 数据长度字节数
        /// </summary>
        private int m_dataLenByteCount = 4;

        /// <summary>
        /// 效验码字节数
        /// </summary>
        private int m_verifyCodeByteCount = 1;

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
            m_dataLen = 0;
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

            byte[] receive = null;

            if (HasReceivedData)
            {
                if (m_dataLen > 0)
                {
                    //表示开头已经接收了消息长度，判断消息长度是否足够即可
                    List<byte> bufferAfterBegin = new List<byte>();
                    bufferAfterBegin.AddRange(ByteReceived);
                    bufferAfterBegin.AddRange(readBuffer);

                    if (bufferAfterBegin.Count >= m_dataLen)
                    {
                        ByteReceived.AddRange(bufferAfterBegin.Take(m_dataLen));

                        if (CheckVerifyCode(ByteReceived))
                        {
                            rest = bufferAfterBegin.Count - m_dataLen;
                            receive = ByteReceived.ToArray();
                            return receive;
                        }
                        else
                        {
                            rest = bufferAfterBegin.Count - m_dataLen;
                            Reset();
                            return null;
                        }
                    }
                    else
                    {
                        ByteReceived.AddRange(bufferAfterBegin);

                        NextReceiveDataFilter = this;
                        rest = 0;
                        return null;
                    }
                }
                else
                {
                    List<byte> bufferAfterBegin = new List<byte>();
                    bufferAfterBegin.AddRange(ByteReceived);
                    bufferAfterBegin.AddRange(readBuffer);
                    if (bufferAfterBegin.Count > (m_beginMark.Length + m_dataLenByteCount))
                    {
                        //获取数据长度的值
                        byte[] dataLenByteArray = bufferAfterBegin.Take(m_beginMark.Length + m_dataLenByteCount).ToArray();
                        dataLenByteArray = dataLenByteArray.Skip(m_beginMark.Length).ToArray();
                        m_dataLen = BitConverter.ToInt32(dataLenByteArray.Reverse().ToArray(), 0);

                        //计算是否接收了完整消息
                        if (bufferAfterBegin.Count >= m_dataLen)
                        {
                            ByteReceived.AddRange(bufferAfterBegin.Take(m_dataLen));

                            if (CheckVerifyCode(ByteReceived))
                            {
                                rest = bufferAfterBegin.Count - m_dataLen;
                                receive = ByteReceived.ToArray();
                                return receive;
                            }
                            else
                            {
                                rest = bufferAfterBegin.Count - m_dataLen;
                                Reset();
                                return null;
                            }
                        }
                        else
                        {
                            ByteReceived.AddRange(bufferAfterBegin);

                            NextReceiveDataFilter = this;
                            rest = 0;
                            return null;
                        }
                    }
                    else
                    {
                        ByteReceived.AddRange(bufferAfterBegin);

                        NextReceiveDataFilter = this;
                        rest = 0;
                        return null;
                    }
                }
            }

            beginIndex = FindStartIndex(readBuffer);
            if (beginIndex == -1)
            {
                rest = 0;
                Reset();

                //如果有异常，可能是接收的数据不符合通信协议，记录日志
                Log("Received data: " + System.Text.Encoding.UTF8.GetString(readBuffer.ToArray()));

                return null;
            }
            else
            {
                //获取数据长度
                //开始标志后的数据长度如果足够长（开始标志长度+数据长度开始位置+数据长度字节数），
                //则获取数据长度信息，如果不够长，则继续接收数据
                List<byte> bufferAfterBegin = readBuffer.Skip(beginIndex).ToList();
                if (bufferAfterBegin.Count > (m_beginMark.Length + m_dataLenByteCount))
                {
                    //获取数据长度的值
                    byte[] dataLenByteArray = bufferAfterBegin.Take(m_beginMark.Length + m_dataLenByteCount).ToArray();
                    dataLenByteArray = dataLenByteArray.Skip(m_beginMark.Length).ToArray();
                    m_dataLen = BitConverter.ToInt32(dataLenByteArray.Reverse().ToArray(), 0);

                    ByteReceived = new List<byte>();

                    //计算是否接收了完整消息
                    if (bufferAfterBegin.Count >= m_dataLen)
                    {
                        ByteReceived.AddRange(bufferAfterBegin.Take(m_dataLen));

                        if (CheckVerifyCode(ByteReceived))
                        {
                            rest = bufferAfterBegin.Count - m_dataLen;
                            receive = ByteReceived.ToArray();
                            return receive;
                        }
                        else
                        {
                            rest = bufferAfterBegin.Count - m_dataLen;
                            Reset();
                            return null;
                        }
                    }
                    else
                    {
                        ByteReceived.AddRange(bufferAfterBegin);

                        NextReceiveDataFilter = this;
                        rest = 0;
                        return null;
                    }
                }
                else
                {
                    ByteReceived = new List<byte>();
                    ByteReceived.AddRange(bufferAfterBegin);

                    NextReceiveDataFilter = this;
                    rest = 0;
                    return null;
                }
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

            int headerLen = m_beginMarkByteCount + m_dataLenByteCount;

            receive = byteReceived.Skip(headerLen).ToArray();
            receive = receive.Take(receive.Length - m_verifyCodeByteCount).ToArray();

            return receive;
        }

        /// <summary>
        /// 打包要发出去的消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <returns>打包后的消息字节数组</returns>
        public byte[] PackageMessage(string message)
        {
            byte[] sendMessage = null;
            byte[] messageSource = this.Encoding.GetBytes(message);

            //数据长度字节数组
            int dataLen = m_beginMarkByteCount + m_dataLenByteCount + messageSource.Length + m_verifyCodeByteCount;
            byte[] byteDataLen = new byte[m_dataLenByteCount];
            byte[] byteDataLenTemp = BitConverter.GetBytes(dataLen).Reverse().ToArray();
            byteDataLenTemp.CopyTo(byteDataLen, 0);

            List<byte> byteBuffer = new List<byte>();
            byteBuffer.AddRange(m_beginMark);
            byteBuffer.AddRange(byteDataLen);
            byteBuffer.AddRange(messageSource);

            //计算校验码
            int verifyCode = 0;
            foreach (byte b in byteBuffer)
            {
                verifyCode ^= b;
            }
            byte[] byteVerifyCode = new byte[m_verifyCodeByteCount];
            byte[] byteVerifyCodeTemp = BitConverter.GetBytes(verifyCode);
            Array.Copy(byteVerifyCodeTemp, byteVerifyCode, 1);

            byteBuffer.AddRange(byteVerifyCode);
            sendMessage = byteBuffer.ToArray();

            return sendMessage;
        }

        /// <summary>
        /// 检查校验码
        /// </summary>
        /// <param name="byteArray"></param>
        /// <returns></returns>
        private bool CheckVerifyCode(List<byte> byteArray)
        {
            //获取校验码的值
            byte[] verifyCodeByteArray = byteArray.Skip(byteArray.Count - m_verifyCodeByteCount).ToArray();
            verifyCodeByteArray = verifyCodeByteArray.Take(m_verifyCodeByteCount).ToArray();
            //if (BitConverter.IsLittleEndian) Array.Reverse(chkSumByteArray);
            byte[] temp = new byte[4];
            Array.Copy(verifyCodeByteArray, temp, 1);
            int verifyCode = BitConverter.ToInt32(temp, 0);

            //计算数据的校验码
            byte[] verifyPart = byteArray.Take(byteArray.Count - m_verifyCodeByteCount).ToArray();
            int verifyCodeCheck = 0;
            foreach (byte b in verifyPart)
            {
                verifyCodeCheck ^= b;
            }

            return verifyCodeCheck == verifyCode;
        }

        /// <summary>
        /// 查找开始标志
        /// </summary>
        /// <param name="readBuffer"></param>
        /// <returns></returns>
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
