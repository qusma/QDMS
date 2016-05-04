using System.ServiceProcess;

namespace QDMSService
{
    public sealed class QdmsService : ServiceBase
    {
        private DataServer _server;

        static void Main(string[] args)
        {
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
