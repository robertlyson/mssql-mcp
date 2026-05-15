# mssql-mcp

A .NET-powered Model Context Protocol (MCP) server for Microsoft SQL Server.

## Abstract

Why does this exist? Because the other MCP solutions in market for this are generally janky pieces of shit that don't work - certainly not on Windows.

This MCP server provides AI agents with robust, reliable access to Microsoft SQL Server databases through a clean, well-architected .NET application using the official MCP C# SDK for protocol compliance.

## Features

- **Schema Discovery**: AI agents can explore database structure without writing complex SQL
- **Query Execution**: Full SQL support for SELECT, INSERT, UPDATE, DELETE, and DDL operations
- **Connection Validation**: Automatic database connectivity validation on startup
- **Error Handling**: Comprehensive error handling with clear, actionable error messages
- **Table Formatting**: Query results formatted in readable tables for AI consumption
- **.NET Tool Support**: Install once and launch directly from MCP client configuration

## Available Tools

| Tool | Description |
|------|-------------|
| `execute_sql` | Execute any SQL query against the database |
| `list_tables` | List all tables with schema, name, type, and row count |
| `list_schemas` | List all available schemas/databases in the SQL Server instance |

## Configuration

### Required Environment Variables

The MCP server requires a single environment variable:

- **`MSSQL_CONNECTION_STRING`**: Complete SQL Server connection string

#### Example Connection Strings

**Windows Authentication:**
```
MSSQL_CONNECTION_STRING="Server=localhost;Database=MyDatabase;Trusted_Connection=true;"
```

**SQL Server Authentication:**
```
MSSQL_CONNECTION_STRING="Server=localhost;Database=MyDatabase;User Id=myuser;Password=mypassword;"
```

**Azure SQL Database:**
```
MSSQL_CONNECTION_STRING="Server=myserver.database.windows.net;Database=mydatabase;User Id=myuser;Password=mypassword;Encrypt=true;"
```

## Running the MCP Server

Install the MCP server as a .NET tool. This lets MCP clients start the server process directly instead of creating a Docker container for every configured database.

```bash
dotnet tool install --global MSSQL.MCP
```

If you are installing from a local checkout:

```bash
dotnet pack src/MSSQL.MCP/MSSQL.MCP.csproj -c Release -o ./artifacts
dotnet tool install --global MSSQL.MCP --add-source ./artifacts
```

After installation, make sure the .NET tools directory is on the PATH seen by your MCP client:

```bash
# macOS/Linux
export PATH="$PATH:$HOME/.dotnet/tools"

# Windows PowerShell
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
```

## MCP Client Configuration

### Cursor IDE

Add to your Cursor settings (`Cursor Settings > Features > Model Context Protocol`):

```json
{
  "mcpServers": {
    "mssql": {
      "command": "mssql-mcp",
      "env": {
        "MSSQL_CONNECTION_STRING": "Server=localhost,1533; Database=MyDb; User Id=myUser; Password=My(!)Password;TrustServerCertificate=true;"
      }
    }
  }
}
```

### Claude Desktop

Add to your Claude Desktop configuration file:

* **Windows:** `%APPDATA%\Claude\claude_desktop_config.json`
* **macOS:** `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "mssql": {
      "command": "mssql-mcp",
      "env": {
        "MSSQL_CONNECTION_STRING": "Server=localhost,1533; Database=MyDb; User Id=myUser; Password=My(!)Password;TrustServerCertificate=true;"
      }
    }
  }
}
```

You might need to create that file and restart Claude Desktop for the changes to take effect.

#### Understanding Your Claude Desktop MCP Server Configuration

This JSON configuration is for **Claude Desktop's Model Context Protocol (MCP) servers**. It essentially teaches Claude how to connect to and use a custom 
"tool" that interacts with a **Microsoft SQL Server (MSSQL)** database.

Let's break down each part:

##### `mcpServers`

This is the top-level section where you define all your custom MCP servers. You can set up multiple servers here, each with its own unique name.

##### `"mssql"`

This is the **unique name** you've chosen for this particular SQL Server integration. Claude will use this name to refer to this database connection.

##### `"command": "mssql-mcp"`

This line tells Claude Desktop to launch the installed .NET tool directly. Each configured database gets its own lightweight MCP process while the client is connected, not a background Docker container.

##### `"env": {...}`

This section defines the **environment variables** that will be set when the MCP client starts the tool.

* `"MSSQL_CONNECTION_STRING": "Server=localhost,1533; Database=MyDb; User Id=myUser; Password=My(!)Password;TrustServerCertificate=true;"`
    * This is the **SQL Server connection string** that the `mssql-mcp` tool uses to connect to your database.
    * Because the server now runs directly on your machine, use the same hostname and port you would use from SSMS, Azure Data Studio, or `sqlcmd`.
    * `Database=MyDb`: The name of the specific database you want to connect to.
    * `User Id=myUser; Password=My(!)Password;`: The credentials for a user (`myUser`) to log into your SQL Server.
    * `TrustServerCertificate=true;`: This tells the client to **skip validating the server's SSL/TLS certificate**. While convenient for development or when 
      using self-signed certificates, be aware this reduces security by making you vulnerable to man-in-the-middle attacks in production environments.

---
##### In a Nutshell:

This configuration enables Claude Desktop to run a SQL Server-specific MCP server as a local .NET tool. This server then uses the provided connection
string to establish a connection to your SQL Server database, allowing Claude to interact with your data through this custom tool.

### Multiple Database Configuration

Add one MCP server entry per database, each with its own connection string. The client starts the .NET tool process when it needs that database connection.

```json
{
  "mcpServers": {
    "orders-db": {
      "command": "mssql-mcp",
      "env": {
        "MSSQL_CONNECTION_STRING": "Server=localhost,1433;Database=Orders;User Id=myUser;Password=My(!)Password;TrustServerCertificate=true;"
      }
    },
    "billing-db": {
      "command": "mssql-mcp",
      "env": {
        "MSSQL_CONNECTION_STRING": "Server=localhost,1433;Database=Billing;User Id=myUser;Password=My(!)Password;TrustServerCertificate=true;"
      }
    }
  }
}
```

## Connection Notes

Since the MCP server runs as a local process, `localhost` means your machine. If SQL Server is running in Docker, publish its port to the host and connect to `localhost,<published-port>`.

## Usage Examples

Once configured, AI agents can use natural language to interact with your database:

**"Show me all the tables in the database"**
→ Uses `list_tables` tool

**"Describe the structure of the Users table"**
→ Uses `execute_sql` with an INFORMATION_SCHEMA query

**"Find all users created in the last 30 days"**
→ Uses `execute_sql` with appropriate SELECT query

**"Create a new customer record"**
→ Uses `execute_sql` with INSERT statement

## Security Considerations

### ⚠️ Important Security Warnings

- **Database Permissions**: Only grant the minimum required permissions to the database user
- **Connection Security**: Use encrypted connections for production environments
- **Access Control**: This MCP server provides full SQL execution capabilities - ensure proper access controls
- **Audit Logging**: Consider enabling SQL Server audit logging for production use
- **Network Security**: Restrict network access to the database server appropriately

### Recommended Database Permissions

For read-only access:
```sql
-- Create a dedicated user with minimal permissions
CREATE LOGIN mcp_readonly WITH PASSWORD = 'SecurePassword123!';
CREATE USER mcp_readonly FOR LOGIN mcp_readonly;

-- Grant only necessary permissions
GRANT SELECT ON SCHEMA::dbo TO mcp_readonly;
GRANT VIEW DEFINITION ON SCHEMA::dbo TO mcp_readonly;
```

For read-write access:
```sql
-- Create a dedicated user
CREATE LOGIN mcp_readwrite WITH PASSWORD = 'SecurePassword123!';
CREATE USER mcp_readwrite FOR LOGIN mcp_readwrite;

-- Grant necessary permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO mcp_readwrite;
GRANT VIEW DEFINITION ON SCHEMA::dbo TO mcp_readwrite;
```

## Troubleshooting

### Connection Issues

1. **Verify connection string**: Test with SQL Server Management Studio or Azure Data Studio
2. **Check firewall**: Ensure SQL Server port (default 1433) is accessible
3. **Enable TCP/IP**: Ensure TCP/IP protocol is enabled in SQL Server Configuration Manager
4. **Authentication mode**: Verify SQL Server is configured for the appropriate authentication mode

### Tool Launch Issues

1. **Command not found**: Ensure `mssql-mcp` is installed and the .NET tools directory is on the MCP client's PATH
2. **Environment variables**: Ensure the connection string is set under the MCP server entry's `env` block
3. **Direct command path**: If your desktop app does not inherit PATH changes, set `command` to the full path of the installed tool

## License

This software is licensed under Apache 2.0 and is available "as is" - this means that if you turbo-nuke your database because you gave an AI agent `sa` access through this MCP server, we're not responsible.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## Architecture

- **[MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)**: Official Model Context Protocol implementation
- **Microsoft.Data.SqlClient**: High-performance SQL Server connectivity

