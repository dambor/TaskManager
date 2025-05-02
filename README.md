# Task Manager with DataStax Enterprise (DSE)

A modern task management application demonstrating how to build scalable applications using C# and DataStax Enterprise.

## Overview

Task Manager is a web-based application that allows users to create, read, update, and delete tasks. Tasks can be assigned to users, categorized by status (To-Do, In Progress, Done), and organized with descriptions. The application leverages DataStax Enterprise (DSE) for high-performance, scalable data storage with advanced security features.

## Features

- Web-based task management interface
- Task creation, editing, and deletion
- Task status tracking (To-Do, In Progress, Done)
- Task assignment to team members
- Responsive design for desktop and mobile devices
- Fault-tolerant data storage with Cassandra/DSE
- Advanced security with DSE Authentication

## Architecture

### Technology Stack

- **Frontend**: HTML, CSS, JavaScript with Bootstrap 5
- **Backend**: ASP.NET Core API
- **Database**: DataStax Enterprise (built on Apache Cassandra)
- **ORM**: Custom repository pattern with DataStax C# Driver

### Component Structure

```
TaskManager/
├── TaskManager.Api/              # ASP.NET Core Web API project
│   ├── Controllers/              # API controllers
│   ├── Program.cs                # Application entry point
│   └── appsettings.json          # Configuration settings
├── TaskManager.Core/             # Core domain models and interfaces
│   ├── Models/                   # Domain models
│   └── Repositories/             # Repository interfaces
├── TaskManager.Infrastructure/   # Implementation of data access
│   └── Repositories/             # Cassandra repository implementations
└── TaskManager.Web/              # Simple frontend (HTML/JS)
    └── index.html                # Main application page
```

## Prerequisites

- .NET 6.0 SDK or later
- DataStax Enterprise 6.8 or later (or Apache Cassandra 4.0+)
- Visual Studio 2022, VS Code, or other compatible IDE

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/task-manager.git
cd task-manager
```

### 2. Configure DataStax Enterprise Connection

Edit the `appsettings.json` file in the TaskManager.Api project to configure your DSE connection:

```json
{
  "CassandraSettings": {
    "ContactPoints": ["localhost"],
    "Keyspace": "task_manager",
    "Username": "cassandra",
    "Password": "cassandra"
  }
}
```

### 3. Build the Solution

```bash
dotnet build
```

### 4. Run the API

```bash
cd TaskManager.Api
dotnet run
```

### 5. Access the Web Interface

Open `TaskManager.Web/index.html` in your web browser, or configure the API URL in the interface to point to your running API instance (default: `http://localhost:5173/api/tasks`).

## Database Schema

The application uses the following tables in the Cassandra/DSE database:

### Tasks Table

```cql
CREATE TABLE tasks (
    id uuid PRIMARY KEY,
    title text,
    description text,
    due_date timestamp,
    status int,
    assigned_to text,
    created_at timestamp,
    updated_at timestamp
);
```

### Tasks By Status (Index Table)

```cql
CREATE TABLE tasks_by_status (
    status int,
    id uuid,
    title text,
    due_date timestamp,
    assigned_to text,
    PRIMARY KEY (status, id)
);
```

### Tasks By Assignee (Index Table)

```cql
CREATE TABLE tasks_by_assignee (
    assigned_to text,
    id uuid,
    title text,
    due_date timestamp,
    status int,
    PRIMARY KEY (assigned_to, id)
);
```

## Key DataStax Enterprise Features Used

- **Multi-datacenter replication**: Configurable through CQL for geographic distribution
- **Enhanced security**: Using DSE authentication providers
- **Efficient data modeling**: Leveraging DSE's optimized storage engine
- **Advanced indexing**: Custom secondary indexes for efficient queries
- **Enterprise-grade operations**: Compatible with DataStax OpsCenter for monitoring

## Using with Apache Cassandra vs. DataStax Enterprise

This application can work with both Apache Cassandra and DataStax Enterprise, with the following considerations:

### Apache Cassandra

- Basic functionality will work with open-source Cassandra
- Limited security features (basic authentication only)
- Standard CQL operations supported
- No advanced features like search or analytics integration

### DataStax Enterprise

- Full functionality including enhanced security
- Support for geospatial data types
- Integration with DSE Search and Analytics when needed
- Advanced authentication mechanisms (LDAP, Kerberos)
- Better performance due to DSE-specific optimizations

To use with Apache Cassandra, modify the `CassandraContext.cs` file to use the standard authentication provider.

## Development

### Adding New Features

1. Define model classes in the Core project
2. Create repository interfaces in the Core project
3. Implement repositories in the Infrastructure project
4. Add controllers in the API project
5. Update the frontend as needed

### Running Tests

```bash
dotnet test
```

## Deployment

### Docker Deployment

A Dockerfile is provided for containerized deployment:

```bash
docker build -t task-manager .
docker run -p 5173:80 task-manager
```

### Cloud Deployment

For cloud deployment on Azure, AWS, or GCP, configure the connection strings to point to your cloud DSE instance.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- DataStax for the C# driver and documentation
- The ASP.NET Core team for the excellent web framework
- Contributors and reviewers who helped improve this project
