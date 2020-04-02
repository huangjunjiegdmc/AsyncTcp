﻿using AsyncTcpServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpServerTest.TcpServer
{
    public class MyTcpServer : AsyncTcpServer<MyServerSession>
    {
        public MyTcpServer(IPAddress ipAddress, int port, IReceiveDataFilter receiveDataFilter, Encoding encoding)
              : base(ipAddress, port, receiveDataFilter, encoding)
        {
        }

        public MyTcpServer(int port, IReceiveDataFilter receiveDataFilter, Encoding encoding) 
            : base(port, receiveDataFilter, encoding)
        {
        }
    }
}
