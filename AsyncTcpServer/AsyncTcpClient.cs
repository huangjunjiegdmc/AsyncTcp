using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncTcpServer
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

        /// <summary>
        /// TCP客户端
        /// </summary>
        private TcpClient m_tcpClient { get; set; }

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

        /// <summary>
        /// 是否已与服务器建立连接
        /// </summary>
        public bool Connected { get { return m_tcpClient.Client.Connected; } }
       
        /// <summary>
        /// 远端服务器终结点
        /// </summary>
        public IPEndPoint RemoteIPEndPoint { get; private set; }

        /// <summary>
        /// 本地客户端终结点
        /// </summary>
        public IPEndPoint LocalIPEndPoint { get; private set; }

        #endregion


        #region 函数

        /// <summary>
        /// 异步TCP客户端
        /// </summary>
        /// <param name="remoteEP"></param>
        public AsyncTcpClient(IPEndPoint remoteEP, IReceiveDataFilter receiveDataFilter, Encoding encoding)
        {
            RemoteIPEndPoint = remoteEP;

            m_tcpClient = new TcpClient();
            m_tcpClient.SendBufferSize = BUFFER_SIZE;
            m_tcpClient.ReceiveBufferSize = BUFFER_SIZE;

            Encoding = encoding;
            ReceiveDataFilter = receiveDataFilter;
            ReceiveDataFilter.Encoding = encoding;
        }

        public AsyncTcpClient(string ip, int port, IReceiveDataFilter receiveDataFilter, Encoding encoding)
        {
            RemoteIPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            m_tcpClient = new TcpClient();
            m_tcpClient.SendBufferSize = BUFFER_SIZE;
            m_tcpClient.ReceiveBufferSize = BUFFER_SIZE;

            Encoding = encoding;
            ReceiveDataFilter = receiveDataFilter;
            ReceiveDataFilter.Encoding = encoding;
        }

        /// <summary>
        /// 异步连接到服务器
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void Connect()
        {
            try
            {
                if (!Connected)
                {
                    if (EnableKeepAlive)
                    {
                        //启用KeepAlive
                        uint dummy = 0;
                        byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
                        BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);//启用Keep-Alive
                        BitConverter.GetBytes((uint)5000).CopyTo(inOptionValues, Marshal.SizeOf(dummy));//KeepAlive间隔：在这个时间间隔内没有数据交互，则发探测包 毫秒
                        BitConverter.GetBytes((uint)500).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);//尝试间隔：发探测包时间间隔 毫秒
                        m_tcpClient.Client.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
                        m_tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    }

                    m_tcpClient.BeginConnect(RemoteIPEndPoint.Address, RemoteIPEndPoint.Port,
                        HandleTcpServerConnected, m_tcpClient);
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message, ex);
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Disconnect()
        {
            try
            {
                OnServerDisconnected();
                m_tcpClient.Close();
            }
            catch (Exception ex)
            {
                Log(ex.Message, ex);
            }
        }

        /// <summary>
        /// 处理连接
        /// </summary>
        /// <param name="ar"></param>
        private void HandleTcpServerConnected(IAsyncResult ar)
        {
            try
            {
                m_tcpClient.EndConnect(ar);

                LocalIPEndPoint = m_tcpClient.Client.LocalEndPoint as IPEndPoint;
                OnServerConnected();

                byte[] buffer = new byte[BUFFER_SIZE];
                m_tcpClient.GetStream().BeginRead(buffer, 0, BUFFER_SIZE, ReceiveCallback, buffer);

            }
            catch (Exception ex)
            {
                Log(ex.Message, ex);
            }
        }

        /// <summary>
        /// 数据接收处理
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                Thread.Sleep(20);//不暂停一下，CPU会占用很高
                Thread.Yield();

                try
                {
                    if (m_tcpClient.Connected)
                    {
                        int length = m_tcpClient.GetStream().EndRead(ar);
                        if (length == 0)
                        {
                            //说明连接已关闭，
                            //这里也可能是由于网络掉包等原因造成读到0个字节，如果是这种情况，则这里不要主动关闭连接

                            OnServerDisconnected();
                            m_tcpClient.Close();

                            Log("Read 0 byte! TcpClient close!");
                            return;
                        }

                        byte[] buffer = (byte[])ar.AsyncState;
                        byte[] receivedBytes = new byte[length];
                        Array.ConstrainedCopy(buffer, 0, receivedBytes, 0, length);

                        HandleDataReceived(receivedBytes);

                        m_tcpClient.GetStream().BeginRead(buffer, 0, BUFFER_SIZE, ReceiveCallback, buffer);
                    }
                }
                catch (System.IO.IOException ioex)
                {
                    //断开连接
                    OnServerDisconnected();
                    m_tcpClient.Close();

                    Log("IOException:" + ioex.Message, ioex);
                    return;
                }
                catch (System.ObjectDisposedException odex)
                {
                    OnServerDisconnected();
                    m_tcpClient.Close();

                    Log("ObjectDisposedException:" + odex.Message, odex);
                    return;
                }
                catch (Exception ex)
                {
                    OnError(ex);
                    OnServerDisconnected();
                    m_tcpClient.Close();

                    Log("Exception:" + ex.Message, ex);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message, ex);
            }
        }

        /// <summary>
        /// 数据接收处理
        /// </summary>
        /// <param name="ar"></param>
        private void HandleDataReceived(byte[] receivedBytes)
        {
            byte[] readBuffer = null;
            List<byte> listReadBuffer = new List<byte>();
            int length = 0;
            int rest = 0;
            byte[] byteReceived = null;

            if (receivedBytes.Length == 0)
            {
                return;
            }

            try
            {
                readBuffer = new byte[BUFFER_SIZE];
                Array.Copy(receivedBytes, readBuffer, receivedBytes.Length);
                length = receivedBytes.Length;
                if (length == BUFFER_SIZE)
                {
                    listReadBuffer.AddRange(readBuffer);
                }
                else
                {
                    listReadBuffer.AddRange(readBuffer.Take(length));
                }

                //处理接收到的数据
                int newrest = 0;
                process_again:
                try
                {
                    if (ReceiveDataFilter.NextReceiveDataFilter != null)
                    {
                        byteReceived = ReceiveDataFilter.NextReceiveDataFilter.Filter(listReadBuffer, out rest);
                    }
                    else
                    {
                        byteReceived = ReceiveDataFilter.Filter(listReadBuffer, out rest);
                    }
                }
                catch (Exception ex)
                {
                    ReceiveDataFilter.Reset();
                    rest = 0;
                    byteReceived = null;

                    Log(ex.Message, ex);
                }


                //不等于空，则说明接收了完整的消息
                if (byteReceived != null)
                {
                    ReceiveDataFilter.Reset();
                    OnRequestReceived(byteReceived);//返回处理
                }

                if (rest == 0)
                {
                    listReadBuffer.Clear();
                }
                else
                {
                    listReadBuffer = new List<byte>(listReadBuffer.Skip(listReadBuffer.Count - rest));

                    //再检查剩下的数据，
                    //如果rest不变，说明剩下的数据中没有符合条件的数据了，之后再从流中读取新的数据。
                    //如果rest修改了，说明剩下的数据有符条件的数据
                    if (newrest != rest)
                    {
                        Logger.Log("rest:" + rest);
                        newrest = rest;
                        goto process_again;
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message, ex);
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(string message)
        {
            try
            {
                byte[] sendMessage = this.ReceiveDataFilter.PackageMessage(message);
                this.m_tcpClient.GetStream().Write(sendMessage, 0, sendMessage.Length);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public delegate void SessionHandler<TEventArgs>(object sender, TEventArgs e);

        public event SessionHandler<ErrorEventArgs> Error;
        public event SessionHandler<EventArgs> ServerDisconnected;
        public event SessionHandler<DataEventArgs> NewRequestReceived;
        public event SessionHandler<EventArgs> ServerConnected;

        protected virtual void OnServerDisconnected()
        {
            ServerDisconnected?.Invoke(this, new EventArgs());
        }

        protected virtual void OnServerConnected()
        {
            ServerConnected?.Invoke(this, new EventArgs());
        }

        protected virtual void OnRequestReceived(byte[] data)
        {
            NewRequestReceived?.Invoke(this, new DataEventArgs(data));
        }

        protected virtual void OnError(Exception e)
        {
            Error?.Invoke(this, new ErrorEventArgs(e));
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

        #endregion
    }
}
