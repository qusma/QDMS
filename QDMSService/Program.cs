using NLog;
using NLog.Targets;
using System.ServiceProcess;

namespace QDMSService
{
    public sealed class QdmsService : ServiceBase
    {
        private DataServer _server;

        static void Main(string[] args)
        {
            if(args != null && args.Length == 1 && args[0] == "--console")
            {
                ColoredConsoleTarget target = new ColoredConsoleTarget();
                target.Layout = "${date:format=HH\\:mm\\:ss}   ${message}";

                NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Trace);
                
                var service = new QdmsService();
                service.OnStart(args);

                while (true)
                { System.Threading.Thread.Sleep(60000); }
            }
            else
                ServiceBase.Run(new QdmsService());
        }

        protected override void OnStart(string[] args)
        {
            Config.DataService config = (Config.DataService)System.Configuration.ConfigurationManager.GetSection("QDMS");
            _server = new DataServer(config);
            _server.Initialisize();
        }

        protected override void OnStop()
        {
            _server.Stop();
        }
    }
}
