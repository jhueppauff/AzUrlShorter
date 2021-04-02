using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace backend.Model
{
    public class Configuration : TableEntity
    {
        public Configuration()
        {

        }


        public Configuration(string configurationKey, string configurationEntry)
        {
            this.PartitionKey = configurationKey;
            this.RowKey = configurationEntry;
        }

        public string Value { get; set; }
    }
}
