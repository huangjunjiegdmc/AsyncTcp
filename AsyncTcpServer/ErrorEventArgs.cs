using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpServer
{
    public class ErrorEventArgs : EventArgs
    {
        private readonly Exception _exception;

        public ErrorEventArgs(Exception exception)
        {
            _exception = exception;
        }

        public Exception Exception => _exception;
    }
}
