using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace backend.Model
{
    public class ShortUrl : TableEntity
    {
        public ShortUrl()
        {

        }

        public ShortUrl(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public string Url { get; set; }
    }
}
