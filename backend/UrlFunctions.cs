using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.Data.Tables;
using System.Collections.Generic;
using backend.Model;
using System.IO;
using Newtonsoft.Json;


namespace Shorter.Backend
{
    public static class UrlFunctions
    {
        [FunctionName(nameof(GetDomains))]
        public static async Task<IActionResult> GetDomains(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Domains")] HttpRequest req,
            [Table("configuration", Connection = "AzureStorageConnection")] TableClient tableClient,
            ILogger log)
        {
            #region Null Checks
            if (tableClient == null)
            {
                throw new ArgumentNullException(nameof(tableClient));
            }
            #endregion

            AsyncPageable<Configuration> queryResults = tableClient.QueryAsync<Configuration>(filter: $"PartitionKey eq 'Domains'");
            List<Configuration> list = new List<Configuration>();

            await foreach (Configuration entity in queryResults)
            {
                list.Add(entity);
            }

            return new OkObjectResult(list);
        }

        [FunctionName(nameof(GetUserLinks))]
        public static async Task<IActionResult> GetUserLinks(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Links")] HttpRequest req,
            [Table("shorturls", Connection = "AzureStorageConnection")] TableClient tableClient,
            ILogger log)
        {
            #region Null Checks
            if (tableClient == null)
            {
                throw new ArgumentNullException(nameof(tableClient));
            }
            #endregion

            AsyncPageable<ShortUrl> queryResults = tableClient.QueryAsync<ShortUrl>(filter: $"UserPrincipleName eq '{StaticWebAppsAuth.Parse(req).Identity.Name}'");

            List<ShortUrl> list = new List<ShortUrl>();

            await foreach (ShortUrl entity in queryResults)
            {
                list.Add(entity);
            }

            return new OkObjectResult(list);
        }

        [FunctionName(nameof(DeleteLink))]
        public static async Task<IActionResult> DeleteLink(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Links/{partitionKey}/{rowKey}")] HttpRequest req, string partitionKey, string rowKey,
            [Table("shorturls", Connection = "AzureStorageConnection")] TableClient tableClient,
            ILogger log)
        {
            #region Null Checks
            if (tableClient == null)
            {
                throw new ArgumentNullException(nameof(tableClient));
            }
            #endregion

            await tableClient.DeleteEntityAsync(partitionKey, rowKey, ETag.All);            
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
