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
            [Table("shorturls")] CloudTable cloudTable,
            ILogger log)
        {
            if (shortUrl == null)
            {
                return new RedirectResult("https://hueppauff.com/notfound", true);
            }
            
            TableQuery<Model.ShortUrl> rangeQuery = new TableQuery<Model.ShortUrl>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, 
                        shortUrl),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, req.Host.Host)));

            IEnumerable<Model.ShortUrl> entity = cloudTable.ExecuteQuery<Model.ShortUrl>(rangeQuery, null);

            if (entity != null && !entity.Any())
            {
                return new RedirectResult("https://hueppauff.com/notfound", true);
            }

            return new RedirectResult(entity.FirstOrDefault().Url, true);
        }
    }
}