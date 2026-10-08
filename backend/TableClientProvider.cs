using System;
using System.Collections.Concurrent;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;

namespace Shorter.Backend
{
    /// <summary>
    /// Creates <see cref="TableClient"/> instances from the configured storage connection.
    /// The functions deliberately resolve storage here instead of through declarative
    /// <c>TableInput</c>/<c>TableOutput</c> bindings: a binding that cannot resolve its
    /// connection fails inside the Functions host and surfaces as an opaque HTTP 500,
    /// whereas this provider lets the functions report what is actually missing.
    /// </summary>
    public sealed class TableClientProvider
    {
        private readonly string _connectionString;
        private readonly ConcurrentDictionary<string, TableClient> _clients = new ConcurrentDictionary<string, TableClient>(StringComparer.Ordinal);

        public TableClientProvider(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // AzureWebJobsStorage is the fallback because the Functions host always has it,
            // which keeps local runs working when only the default setting is present.
            _connectionString = Coalesce(
                configuration["AzureStorageConnection"],
                configuration["Values:AzureStorageConnection"],
                configuration["AzureWebJobsStorage"]);
        }

        /// <summary>
        /// False when neither AzureStorageConnection nor AzureWebJobsStorage is configured,
        /// which is the usual cause of a freshly deployed Static Web App failing every call.
        /// </summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

        public TableClient GetTableClient(string tableName)
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException("No storage connection string is configured.");
            }

            return _clients.GetOrAdd(tableName, name => new TableClient(_connectionString, name));
        }

        private static string Coalesce(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }
    }
}
