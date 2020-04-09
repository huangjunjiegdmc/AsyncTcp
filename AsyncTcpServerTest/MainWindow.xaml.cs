using AsyncTcpServer;
using AsyncTcpServerTest.TcpServer;
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

namespace AsyncTcpServerTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        MyTcpServer myTcpServer = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MyLogger myLogger = new MyLogger();
            BeginEndFilter receiveDataFilter = new BeginEndFilter();
            myTcpServer = new MyTcpServer(4000, receiveDataFilter, Encoding.UTF8, myLogger);
            myTcpServer.EnableKeepAlive = true;
            myTcpServer.NewSessionConnected += MyTcpServer_NewSessionConnected;
            myTcpServer.SessionClosed += MyTcpServer_SessionClosed;
            myTcpServer.NewRequestReceived += MyTcpServer_NewRequestReceived;
            myTcpServer.Error += MyTcpServer_Error;
            myTcpServer.Start();

            string message = "Server start on:" + myTcpServer.ServerIP
                       + ":" + myTcpServer.ServerPort;
            myTcpServer.Logger.Log(message);
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    txt.AppendText(message + "\r\n\r\n");
                    txt.ScrollToEnd();
                }));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            string message = "Server stop on:" + myTcpServer.ServerIP
                       + ":" + myTcpServer.ServerPort;
            myTcpServer.Logger.Log(message);
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    txt.AppendText(message + "\r\n\r\n");
                    txt.ScrollToEnd();
                }));

            myTcpServer.Stop();
        }

        private void MyTcpServer_Error(object sender, ErrorEventArgs e)
        {
            MyServerSession myServerSession = sender as MyServerSession;
            MyTcpServer myTcpServer = myServerSession.Server as MyTcpServer;
            string message = "Error occur on:" + myServerSession.IpEndPoint.Address.ToString()
                       + ":" + myServerSession.IpEndPoint.Port + ", "+ e.Exception.Message;
            myTcpServer.Logger.Log(message);
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    txt.AppendText(message + "\r\n\r\n");
                    txt.ScrollToEnd();
                }));
        }

        private void MyTcpServer_NewRequestReceived(object sender, DataEventArgs e)
        {
            MyServerSession myServerSession = sender as MyServerSession;
            MyTcpServer myTcpServer = myServerSession.Server as MyTcpServer;
            byte[] pureData = myTcpServer.ReceiveDataFilter.RemoveMessageFilterFlag(e.Data);
            string messageReceivedOrigin = myTcpServer.Encoding.GetString(e.Data);
            string message = myTcpServer.Encoding.GetString(pureData);
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
        }

        private void MyTcpServer_SessionClosed(object sender, EventArgs e)
        {
            MyServerSession myServerSession = sender as MyServerSession;
            MyTcpServer myTcpServer = myServerSession.Server as MyTcpServer;
            string message = "Client disconnected:" + myServerSession.IpEndPoint.Address.ToString()
                        + ":" + myServerSession.IpEndPoint.Port;
            myTcpServer.Logger.Log(message);
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    txt.AppendText(message + "\r\n\r\n");
                    txt.ScrollToEnd();
                }));
        }

        private void MyTcpServer_NewSessionConnected(object sender, EventArgs e)
        {
            MyServerSession myServerSession = sender as MyServerSession;
            MyTcpServer myTcpServer = myServerSession.Server as MyTcpServer;
            string message = "New client connected:" + myServerSession.IpEndPoint.Address.ToString()
                       + ":" + myServerSession.IpEndPoint.Port;
            myTcpServer.Logger.Log(message);
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    txt.AppendText(message + "\r\n\r\n");
                    txt.ScrollToEnd();
                }));
        }
    }
}
