using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace TaskManager.Infrastructure
{
    public class CassandraInitializationService : IHostedService
    {
        private readonly CassandraContext _cassandraContext;

        public CassandraInitializationService(CassandraContext cassandraContext)
        {
            _cassandraContext = cassandraContext;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cassandraContext.InitializeSchema();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}