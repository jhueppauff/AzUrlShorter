using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Text;

namespace frontend.Model
{
    public class ConfigurationEntry
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }

        public string Value { get; set; }
    }
}
