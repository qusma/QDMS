using QDMS.Server;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QDMS.Server.Services
{
    public interface IDatasourceService
    {
        List<RealTimeStreamInfo> GetActiveStreams();
        Task<List<Datasource>> GetAll(CancellationToken token);
        List<DataSourceStatus> GetDatasourceStatus();
    }
}