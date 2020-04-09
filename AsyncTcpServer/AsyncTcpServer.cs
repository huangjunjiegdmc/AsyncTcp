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
    /// 异步TCP服务器
    /// </summary>
    public class AsyncTcpServer<T> where T : ServerSessionBase<T>, new()
    {
        #region 常量

        /// <summary>
        /// 定义缓冲区
        /// </summary>
        public const int BUFFER_SIZE = 1024 * 1024;

        #endregion



        #region 变量

        /// <summary>
        /// 服务端Socet
        /// </summary>
        private TcpListener m_serverListener;

        /// <summary>
        /// 会话列表
        /// </summary>
        private List<T> m_sessionList = new List<T>();

        #endregion



        #region 属性

        /// <summary>
        /// 服务器IP
        /// </summary>
        public string ServerIP { get; private set; }

        /// <summary>
        /// 服务器端口
        /// </summary>
        public int ServerPort { get; set; }

        /// <summary>
        /// 是否运行
        /// </summary>
        public bool IsRunning { get; private set; }

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




        #region 函数

        /// <summary>
        /// 构造函数，指定地址和端口
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口号</param>
        public AsyncTcpServer(IPAddress ipAddress, int port, IReceiveDataFilter receiveDataFilter, Encoding encoding)
        {
            this.ServerIP = ipAddress.ToString();
            this.ServerPort = port;
            m_serverListener = new TcpListener(ipAddress, port);
            Encoding = encoding;
            ReceiveDataFilter = receiveDataFilter;
            ReceiveDataFilter.Encoding = encoding;
        }
        
        /// <summary>
        /// 构造函数，指定端口，默认监听地址0.0.0.0
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="root">根目录</param>
        public AsyncTcpServer(int port, IReceiveDataFilter receiveDataFilter, Encoding encoding)
        {
            this.ServerIP = IPAddress.Any.ToString();
            this.ServerPort = port;
            m_serverListener = new TcpListener(IPAddress.Any, port);
            Encoding = encoding;
            ReceiveDataFilter = receiveDataFilter;
            ReceiveDataFilter.Encoding = encoding;
        }

        /// <summary>
        /// 获取客户端会话列表
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> GetAllSessions()
        {
            return m_sessionList;
        }

        /// <summary>
        /// 通过会话ID获取会话信息
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <returns></returns>
        public T GetSessionById(string sessionId)
        {
            T t = null;

            if (m_sessionList.Any(p => p.SessionID == sessionId))
            {
                t = m_sessionList.Where(p => p.SessionID == sessionId).First();
            }

            return t;
        }

        /// <summary>
        /// 启动TCP监听服务
        /// </summary>
        public void Start()
        {
            try
            {
                if (!IsRunning)
                {
                    IsRunning = true;

                    if (EnableKeepAlive)
                    {
                        //启用KeepAlive
                        uint dummy = 0;
                        byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
                        BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);//启用Keep-Alive
                        BitConverter.GetBytes((uint)3000).CopyTo(inOptionValues, Marshal.SizeOf(dummy));//KeepAlive间隔：在这个时间间隔内没有数据交互，则发探测包 毫秒
                        BitConverter.GetBytes((uint)500).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);//尝试间隔：发探测包时间间隔 毫秒
                        m_serverListener.Server.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
                        m_serverListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    }

                    m_serverListener.Start();

                    m_serverListener.BeginAcceptTcpClient(
                    new AsyncCallback(HandleTcpClientAccepted), m_serverListener);
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message, ex);
            }
        }

        /// <summary>
        /// 停止TCP监听服务
        /// </summary>
        /// <returns></returns>
        public void Stop()
        {
            try
            {
                if (IsRunning)
                {
                    IsRunning = false;

                    for (int i = 0; i < this.m_sessionList.Count; i++)
                    {
                        this.m_sessionList[i].TcpClient.Close();
                    }

                    lock (m_sessionList)
                    {
                        this.m_sessionList.Clear();
                    }

                    m_serverListener.Stop();
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message, ex);
            }
        }

        /// <summary>
        /// 处理新连接
        /// </summary>
        /// <param name="ar"></param>
        private void HandleTcpClientAccepted(IAsyncResult ar)
        {
            if (IsRunning)
            {
                try
                {
                    TcpListener tcpListener = (TcpListener)ar.AsyncState;
                    TcpClient client = tcpListener.EndAcceptTcpClient(ar);

                    T serverSession = new T()
                    {
                        SessionID = Guid.NewGuid().ToString("N").ToUpper(),
                        ConnectTime = DateTime.Now,
                        TcpClient = client,
                        IpEndPoint = client.Client.RemoteEndPoint as IPEndPoint,
                        Server = this
                    };

                    //Log("New client connected:" + serverSession.IpEndPoint.Address.ToString()
                    //    + ":" + serverSession.IpEndPoint.Port);

                    lock (m_sessionList)
                    {
                        m_sessionList.Add(serverSession);
                    }

                    OnNewSessionConnected(serverSession);

                    byte[] buffer = new byte[BUFFER_SIZE];
                    ClientState clientState = new ClientState(serverSession, buffer);
                    client.GetStream().BeginRead(clientState.Buffer, 0, BUFFER_SIZE,
                        ReceiveCallback, clientState);

                    tcpListener.BeginAcceptTcpClient(new AsyncCallback(HandleTcpClientAccepted), ar.AsyncState);
                }
                catch (Exception ex)
                {
                    Log(ex.Message, ex);
                }
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
                ClientState clientState = (ClientState)ar.AsyncState;
                T session = clientState.ServerSession as T;

                Thread.Sleep(20);//不暂停一下，CPU会占用很高
                Thread.Yield();

                try
                {
                    if (session.TcpClient.Connected)
                    {
                        int length = session.TcpClient.GetStream().EndRead(ar);
                        if (length == 0)
                        {
                            //说明连接已关闭，
                            //这里也可能是由于网络掉包等原因造成读到0个字节，如果是这种情况，则这里不要主动关闭连接
                            lock (m_sessionList)
                            {
                                m_sessionList.Remove(session);
                            }

                            OnSessionClosed(session);
                            session.TcpClient.Close();

                            Log("Read 0 byte! TcpClient close!");
                            return;
                        }
                        byte[] receivedBytes = new byte[length];
                        Array.ConstrainedCopy(clientState.Buffer, 0, receivedBytes, 0, length);
                        HandleDataReceived(session, receivedBytes);

                        session.TcpClient.GetStream().BeginRead(clientState.Buffer, 0, BUFFER_SIZE,
                            ReceiveCallback, clientState);
                    }
                }
                catch (System.IO.IOException ioex)
                {
                    //断开连接
                    lock (m_sessionList)
                    {
                        m_sessionList.Remove(session);
                    }

                    OnSessionClosed(session);
                    session.TcpClient.Close();

                    Log("IOException:" + ioex.Message, ioex);
                    return;
                }
                catch (System.ObjectDisposedException odex)
                {
                    lock (m_sessionList)
                    {
                        m_sessionList.Remove(session);
                    }

                    OnSessionClosed(session);
                    session.TcpClient.Close();

                    Log("ObjectDisposedException:" + odex.Message, odex);
                    return;
                }
                catch (Exception ex)
                {
                    lock (m_sessionList)
                    {
                        m_sessionList.Remove(session);
                    }

                    OnError(session, ex);
                    OnSessionClosed(session);
                    session.TcpClient.Close();

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
        private void HandleDataReceived(T session, byte[] receivedBytes)
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
                    OnRequestReceived(session, byteReceived);//返回处理
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

        public delegate void SessionHandler<TT, TEventArgs>(object sender, TEventArgs e);

        public event SessionHandler<T, ErrorEventArgs> Error;
        public event SessionHandler<T, EventArgs> SessionClosed;
        public event SessionHandler<T, DataEventArgs> NewRequestReceived;
        public event SessionHandler<T, EventArgs> NewSessionConnected;

        protected virtual void OnSessionClosed(T serverSession)
        {
            SessionClosed?.Invoke(serverSession, new EventArgs());
        }

        protected virtual void OnNewSessionConnected(T serverSession)
        {
            NewSessionConnected?.Invoke(serverSession, new EventArgs());
        }

        protected virtual void OnRequestReceived(T serverSession, byte[] data)
        {
            NewRequestReceived?.Invoke(serverSession, new DataEventArgs(data));
        }

        protected virtual void OnError(T serverSession, Exception e)
        {
            Error?.Invoke(serverSession, new ErrorEventArgs(e));
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
