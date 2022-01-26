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
using System.Security.Claims;

namespace Shorter.Backend
{
    public static class UrlFunctions
    {
        [FunctionName(nameof(GetDomains))]
        public static async Task<IActionResult> GetDomains(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Domains")] HttpRequest req,
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

        [FunctionName(nameof(GetUserLinks))]
        public static async Task<IActionResult> GetUserLinks(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Links")] HttpRequest req,
            [Table("shorturls", Connection = "AzureStorageConnection")] CloudTable cloudTable,
            ILogger log)
        {
            #region Null Checks
            if (cloudTable == null)
            {
                throw new ArgumentNullException(nameof(cloudTable));
            }
            #endregion

            var query = new TableQuery<ShortUrl>().Where(TableQuery.GenerateFilterCondition("UserPrincipleName", QueryComparisons.Equal, StaticWebAppsAuth.Parse(req).Identity.Name));

            List<ShortUrl> list = new List<ShortUrl>();

            foreach (var entity in await cloudTable.ExecuteQuerySegmentedAsync(query, null).ConfigureAwait(false))
            {
                list.Add(entity);
            }

            return new OkObjectResult(list);
        }

        [FunctionName(nameof(DeleteLink))]
        public static async Task<IActionResult> DeleteLink(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Links/{partitionKey}/{rowKey}")] HttpRequest req, string partitionKey, string rowKey,
            [Table("shorturls", Connection = "AzureStorageConnection")] CloudTable cloudTable,
            ILogger log)
        {
            #region Null Checks
            if (cloudTable == null)
            {
                throw new ArgumentNullException(nameof(cloudTable));
            }
            #endregion

            var entity = new ShortUrl
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                ETag = "*"
            };

            TableOperation deleteOperation = TableOperation.Delete(entity);
            await cloudTable.ExecuteAsync(deleteOperation).ConfigureAwait(false);
            
            return new OkResult();
        }

        [FunctionName(nameof(IngestShortLink))]
        [return: Table("shorturls", Connection = "AzureStorageConnection")]
        public async static Task<ShortUrl> IngestShortLink([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Links")] HttpRequest req,
        ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            ShortUrl data = JsonConvert.DeserializeObject<ShortUrl>(requestBody);

            data.UserPrincipleName = StaticWebAppsAuth.Parse(req).Identity.Name;

            return data;
        }
    }
}