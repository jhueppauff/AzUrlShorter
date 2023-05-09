using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using Azure;
using System.Threading.Tasks;

namespace AzUrlShorter.Redirect
{
    public static class Main
    {
        [FunctionName(nameof(Redirect))]
        public async static Task<IActionResult> Redirect(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/{shortUrl}")] HttpRequest req, string shortUrl,
            [Table("shorturls", Connection = "AzureStorageConnection")] TableClient tableClient,
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

            AsyncPageable<Model.ShortUrl> queryResults = tableClient.QueryAsync<Model.ShortUrl>(filter: $"PartitionKey eq '{shortUrl}' and RowKey eq '{originHost}'");

            List<Model.ShortUrl> shortUrls= new List<Model.ShortUrl>();
            await foreach (Model.ShortUrl entity in queryResults)
            {
                shortUrls.Add(entity);
            }

            if (shortUrls.Count == 0)
            {
                log.LogError($"No ShortUrl was found");
                return new RedirectResult("https://hueppauff.com/notfound", true);
            }

            return new RedirectResult(shortUrls.FirstOrDefault().Url, true);
        }
    }
}