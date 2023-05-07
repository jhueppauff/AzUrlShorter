using Azure;
using Azure.Data.Tables;
using System;

namespace backend.Model
{
    public class Configuration : ITableEntity
    {
        public string Value { get; set; }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
