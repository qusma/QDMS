using QDMSApp;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QDMS.Server.Services
{
    public interface IDatasourceService
    {
        ConcurrentNotifierBlockingList<RealTimeStreamInfo> GetActiveStreams();
        Task<List<Datasource>> GetAll(CancellationToken token);
        List<DataSourceStatus> GetDatasourceStatus();
    }
}