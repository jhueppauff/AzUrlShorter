using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace frontend.Model
{
    public class ConfigurationEntry : TableEntity
    {
        public ConfigurationEntry()
        {
        }


        public ConfigurationEntry(string configurationKey, string configurationEntry)
        {
            this.PartitionKey = configurationKey;
            this.RowKey = configurationEntry;
        }

        public string Value { get; set; }
    }
}
