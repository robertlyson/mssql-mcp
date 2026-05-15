using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MSSQL.MCP.Database;

namespace MSSQL.MCP.Services;

public sealed class DatabaseValidationHostedService(
    ISqlConnectionFactory connectionFactory,
    ILogger<DatabaseValidationHostedService> logger) : IHostedService
{
    private static readonly TimeSpan ValidationTimeout = TimeSpan.FromSeconds(30);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting database connection validation...");

        using var timeout = new CancellationTokenSource(ValidationTimeout);
        using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeout.Token);

        try
        {
            var isValid = await connectionFactory.ValidateConnectionAsync(linkedToken.Token);
            if (!isValid)
            {
                throw new InvalidOperationException("Unable to connect to the database.");
            }

            logger.LogInformation("Database connection validated successfully. Ready to process MCP requests.");
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            logger.LogError(
                "Database connection validation timed out after {TimeoutSeconds} seconds.",
                ValidationTimeout.TotalSeconds);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database connection validation failed: {Message}", ex.Message);
            logger.LogError("Please verify that MSSQL_CONNECTION_STRING is set correctly, SQL Server is accessible, the database exists, and credentials are valid.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
