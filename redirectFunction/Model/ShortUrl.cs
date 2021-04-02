using Microsoft.Azure.Cosmos.Table;

namespace AzUrlShorter.Redirect.Model
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