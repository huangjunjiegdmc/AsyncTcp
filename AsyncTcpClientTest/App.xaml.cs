using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace AsyncTcpClientTest
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


        protected override void OnStartup(StartupEventArgs e)
        {
            log4net.Config.XmlConfigurator.Configure();
            log.Debug("");
            log.Debug("<====================logger start====================>");

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            log.Debug("<====================logger  end ====================>");
            log.Debug("");
        }
    }
}
