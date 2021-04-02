using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using backend.Model;
using System.IO;
using Newtonsoft.Json;

namespace Shorter.Backend
{
    public static class ListOperators
    {
        [FunctionName(nameof(GetDomains))]
        public static async Task<IActionResult> GetDomains(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Table("configuration", Connection = "AzureStorageConnection")] CloudTable cloudTable,
            ILogger log)
        {
            #region Null Checks
            if (cloudTable == null)
            {
                throw new ArgumentNullException(nameof(cloudTable));
            }
            #endregion

            var query = new TableQuery<Configuration>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Domains"));

            List<Configuration> list = new List<Configuration>();

            foreach (var entity in await cloudTable.ExecuteQuerySegmentedAsync(query, null).ConfigureAwait(false))
            {
                list.Add(entity);
            }

            return new OkObjectResult(list);
        }

        [FunctionName(nameof(Ingest))]
        [return: Table("shorturls", Connection = "AzureStorageConnection")]
        public async static Task<ShortUrl> Ingest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req, ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            var Host = req.Host;

            return new ShortUrl((string)data.ShortUrl, Host.Host)
            {
                Url = (string)data.Url
            };
        }
    }
}
