using Cassandra;
using Microsoft.Extensions.Configuration;
using System;

namespace TaskManager.Infrastructure
{
    public class CassandraContext : IDisposable
    {
        private Cluster _cluster;
        public ISession Session { get; private set; }

        public CassandraContext(IConfiguration configuration)
        {
            var contactPoints = configuration.GetSection("CassandraSettings:ContactPoints").Get<string[]>();
            var keyspace = configuration["CassandraSettings:Keyspace"];
            var username = configuration["CassandraSettings:Username"];
            var password = configuration["CassandraSettings:Password"];

            _cluster = Cluster.Builder()
                .AddContactPoints(contactPoints)
                .WithAuthProvider(new PlainTextAuthProvider(username, password))
                .Build();

            // Connect to the system keyspace first to create our keyspace if needed
            var systemSession = _cluster.Connect();
            
            // Create keyspace if it doesn't exist
            systemSession.Execute(@"
                CREATE KEYSPACE IF NOT EXISTS task_manager
                WITH REPLICATION = { 'class' : 'SimpleStrategy', 'replication_factor' : 1 };
            ");
            
            systemSession.Dispose();
            
            // Now connect to our keyspace
            Session = _cluster.Connect(keyspace);
        }

        public void Dispose()
        {
            Session?.Dispose();
            _cluster?.Dispose();
        }

        public void InitializeSchema()
        {
            // Create tasks table if it doesn't exist
            Session.Execute(@"
                CREATE TABLE IF NOT EXISTS tasks (
                    id uuid PRIMARY KEY,
                    title text,
                    description text,
                    due_date timestamp,
                    status int,
                    assigned_to text,
                    created_at timestamp,
                    updated_at timestamp
                );
            ");

            // Create tasks_by_status index table
            Session.Execute(@"
                CREATE TABLE IF NOT EXISTS tasks_by_status (
                    status int,
                    id uuid,
                    title text,
                    due_date timestamp,
                    assigned_to text,
                    PRIMARY KEY (status, id)
                );
            ");

            // Create tasks_by_assignee index table
            Session.Execute(@"
                CREATE TABLE IF NOT EXISTS tasks_by_assignee (
                    assigned_to text,
                    id uuid,
                    title text,
                    due_date timestamp,
                    status int,
                    PRIMARY KEY (assigned_to, id)
                );
            ");
        }
    }
}