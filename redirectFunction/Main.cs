using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using Azure;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;

namespace AzUrlShorter.Redirect
{
    public class Main
    {
        private readonly ILogger _logger;

        public Main(ILogger<Main> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Redirect))]
        public async Task<IActionResult> Redirect(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "/{shortUrl}")] HttpRequest req, string shortUrl, [TableInput("shorturls", Connection = "AzureStorageConnection")] TableClient tableClient)
        {
            if (shortUrl == null)
            {
                _logger.LogError($"No ShortUrl was specified");
                return new RedirectResult("https://hueppauff.com/notfound", true);
            }

            string originHost = req.Headers.ContainsKey("cdn-origin") ? req.Headers["cdn-origin"] : req.Headers["host"];
            originHost = originHost.Split(':')[0].Trim();
            _logger.LogInformation($"Request for domain {originHost}");

            AsyncPageable<Model.ShortUrl> queryResults = tableClient.QueryAsync<Model.ShortUrl>(filter: $"PartitionKey eq '{shortUrl}' and RowKey eq '{originHost}'");

            List<Model.ShortUrl> shortUrls = new List<Model.ShortUrl>();
            await foreach (Model.ShortUrl entity in queryResults)
            {
                shortUrls.Add(entity);
            }

            if (shortUrls.Count == 0)
            {
                _logger.LogError($"No ShortUrl was found");
                return new RedirectResult("https://hueppauff.com/notfound", true);
            }

            return new RedirectResult(shortUrls.FirstOrDefault().Url, true);
        }
    }
}