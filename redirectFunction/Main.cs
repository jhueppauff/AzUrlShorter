using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Azure.Data.Tables;
using Azure;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.ApplicationInsights;
using System;

namespace AzUrlShorter.Redirect
{
    public class Main(ILogger<Main> logger, TelemetryClient telemetry)
    {
        private readonly ILogger<Main> _logger = logger;

        [Function(nameof(Redirect))]
        public async Task<IActionResult> Redirect(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{shortUrl}")] HttpRequest req, string shortUrl, 
        [TableInput("shorturls", Connection = "AzureStorageConnection")] TableClient tableClient)
        {
            _logger.LogTrace("Triggered redirect function, shortUrl: {shortUrl}", shortUrl);
            if (shortUrl == null)
            {
                _logger.LogError($"No ShortUrl was specified");
                return new RedirectResult("https://hueppauff.com/notfound", true);
            }

            string originHost = req.Headers.ContainsKey("cdn-origin") ? req.Headers["cdn-origin"] : req.Headers["host"];
            originHost = originHost.Split(':')[0].Trim();
            _logger.LogTrace($"Request for domain {originHost}");

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

            Model.ShortUrl link = shortUrls.First();

            try
            {
                // The existing SDK buffers telemetry; never flush or await delivery here.
                var properties = new Dictionary<string, string>
                {
                    ["shortKey"] = link.PartitionKey,
                    ["domain"] = link.RowKey
                };
                if (!string.IsNullOrEmpty(link.TrackingId))
                {
                    properties["trackingId"] = link.TrackingId;
                }
                telemetry.TrackEvent("ShortLinkUsed", properties);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not record link usage.");
            }

            return new RedirectResult(link.Url, true);
        }
    }
}