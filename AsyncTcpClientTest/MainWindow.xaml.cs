using AsyncTcpClientTest.TcpServer;
using AsyncTcpServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AsyncTcpClientTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        AsyncTcpClient asyncTcpClient = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MyLogger myLogger = new MyLogger();
            BeginEndFilter receiveDataFilter = new BeginEndFilter();
            asyncTcpClient = new AsyncTcpClient(
                new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 4000), receiveDataFilter,
                Encoding.UTF8, myLogger);
            asyncTcpClient.EnableKeepAlive = true;
            asyncTcpClient.ServerConnected += AsyncTcpClient_ServerConnected;
            asyncTcpClient.ServerDisconnected += AsyncTcpClient_ServerDisconnected;
            asyncTcpClient.NewRequestReceived += AsyncTcpClient_NewRequestReceived;
            asyncTcpClient.Error += AsyncTcpClient_Error;
            asyncTcpClient.Connect();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (asyncTcpClient.Connected)
                asyncTcpClient.Disconnect();
        }

        private void AsyncTcpClient_Error(object sender, ErrorEventArgs e)
        {
            AsyncTcpClient asyncTcpClient = sender as AsyncTcpClient;
            string message = "Error occur on:" + asyncTcpClient.LocalIPEndPoint.Address.ToString()
                       + ":" + asyncTcpClient.LocalIPEndPoint.Port + ", " + e.Exception.Message;
            asyncTcpClient.Logger.Log(message);
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    txt.AppendText(message + "\r\n\r\n");
                    txt.ScrollToEnd();
                }));
        }

        private void AsyncTcpClient_NewRequestReceived(object sender, DataEventArgs e)
        {
            AsyncTcpClient asyncTcpClient = sender as AsyncTcpClient;
            byte[] pureData = asyncTcpClient.ReceiveDataFilter.RemoveMessageFilterFlag(e.Data);
            string messageReceivedOrigin = asyncTcpClient.Encoding.GetString(e.Data);
            string message = asyncTcpClient.Encoding.GetString(pureData);
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    if (message.Length > 2000)
                    {
                        txt.AppendText(message.Substring(0, 2000) + "\r\n......\r\n\r\n");
                    }
                    else
                    {
                        txt.AppendText(message + "\r\n\r\n");
                    }
                    txt.ScrollToEnd();
                }));

            asyncTcpClient.SendMessage("Message received!");
        }

        private void AsyncTcpClient_ServerDisconnected(object sender, EventArgs e)
        {
            AsyncTcpClient asyncTcpClient = sender as AsyncTcpClient;
            string message = "Server disconnected on:" + asyncTcpClient.LocalIPEndPoint.Address.ToString()
                       + ":" + asyncTcpClient.LocalIPEndPoint.Port;
            asyncTcpClient.Logger.Log(message);
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    txt.AppendText(message + "\r\n\r\n");
                    txt.ScrollToEnd();
                }));
        }

        private void AsyncTcpClient_ServerConnected(object sender, EventArgs e)
        {
            AsyncTcpClient asyncTcpClient = sender as AsyncTcpClient;
            string message = "Server connected on:" + asyncTcpClient.LocalIPEndPoint.Address.ToString()
                       + ":" + asyncTcpClient.LocalIPEndPoint.Port;
            asyncTcpClient.Logger.Log(message);
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    txt.AppendText(message + "\r\n\r\n");
                    txt.ScrollToEnd();
                }));
            asyncTcpClient.SendMessage("Hello server!");
        }
    }
}
