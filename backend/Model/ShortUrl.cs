using Azure;
using Azure.Data.Tables;
using System;

namespace backend.Model
{
    public class ShortUrl : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Url { get; set; }

        public string UserPrincipleName { get; set; }
    }
}
