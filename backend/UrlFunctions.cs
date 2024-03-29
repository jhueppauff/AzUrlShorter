using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.Data.Tables;
using System.Collections.Generic;
using backend.Model;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker;

namespace Shorter.Backend
{
    public class UrlFunctions
    {
        private readonly ILogger _logger;

        public UrlFunctions(ILogger<UrlFunctions> logger)
        {
            _logger = logger;
        }

        [Function(nameof(GetDomains))]
        public async Task<IActionResult> GetDomains(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Domains")] HttpRequest req, [TableInput("configuration", Connection = "AzureStorageConnection")] TableClient tableClient)
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

        [Function(nameof(GetUserLinks))]
        public static async Task<IActionResult> GetUserLinks(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Links")] HttpRequest req,
            [TableInput("shorturls", Connection = "AzureStorageConnection")] TableClient tableClient,
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

        [Function(nameof(DeleteLink))]
        public static async Task<IActionResult> DeleteLink(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Links/{partitionKey}/{rowKey}")] HttpRequest req, string partitionKey, string rowKey,
            [TableInput("shorturls", Connection = "AzureStorageConnection")] TableClient tableClient,
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

        [Function(nameof(IngestShortLink))]
        [TableOutput("shorturls", Connection = "AzureStorageConnection")]
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
