using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Linq;

namespace AzUrlShorter.Redirect
{
    public static class Main
    {
        [FunctionName(nameof(Redirect))]
        public static IActionResult Redirect(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "redirect/{shortUrl}")] HttpRequest req, string shortUrl,
            [Table("shorturls", Connection = "AzureStorageConnection")] CloudTable cloudTable,
            ILogger log)
        {
            if (shortUrl == null)
            {
                log.LogError($"No ShortUrl was specified");
                return new RedirectResult("https://hueppauff.com/notfound", true);
            }

            string originHost = req.Headers.ContainsKey("cdn-origin") ? req.Headers["cdn-origin"] : req.Headers["host"];
            originHost = originHost.Split(':')[0].Trim();

            log.LogInformation($"Request for domain {originHost}");


            TableQuery<Model.ShortUrl> rangeQuery = new TableQuery<Model.ShortUrl>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, 
                        shortUrl),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, originHost)));

            IEnumerable<Model.ShortUrl> entity = cloudTable.ExecuteQuery<Model.ShortUrl>(rangeQuery, null);

            if (entity != null && !entity.Any())
            {
                log.LogError($"No ShortUrl was found");
                return new RedirectResult("https://hueppauff.com/notfound", true);
            }

            return new RedirectResult(entity.FirstOrDefault().Url, true);
        }
    }
}