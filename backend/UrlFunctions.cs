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
        private const string ShortUrlTable = "shorturls";
        private const string ConfigurationTable = "configuration";

        private readonly ILogger<UrlFunctions> _logger;
        private readonly TableClientProvider _tables;
        private readonly LinkUsageProvider _usage;

        public UrlFunctions(ILogger<UrlFunctions> logger, TableClientProvider tables, LinkUsageProvider usage)
        {
            _logger = logger;
            _tables = tables;
            _usage = usage;
        }

        /// <summary>
        /// Returns the signed-in user, or <c>null</c> when the request carries no usable
        /// Static Web Apps principal. <see cref="StaticWebAppsAuth.Parse"/> returns
        /// <c>null</c> without the header and an identity-less principal without roles.
        /// </summary>
        private static string GetUserName(HttpRequest req) =>
            StaticWebAppsAuth.Parse(req)?.Identity?.Name;

        /// <summary>Escapes a value so it can be embedded in an OData string literal.</summary>
        private static string EscapeODataLiteral(string value) => value.Replace("'", "''");

        /// <summary>
        /// Mirrors the destination check the frontend performs, so a crafted request cannot
        /// store a `javascript:` or other non-web target for the redirect function to serve.
        /// </summary>
        private static bool IsSafeTargetUrl(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out Uri parsed)
            && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);

        /// <summary>
        /// Returns an explanatory error when storage is not configured, so the cause is
        /// visible in the UI instead of appearing as an unexplained HTTP 500.
        /// </summary>
        private IActionResult CheckStorageConfigured(string functionName)
        {
            if (_tables.IsConfigured)
            {
                return null;
            }

            _logger.LogError(
                "{Function} cannot run: neither the 'AzureStorageConnection' nor the 'AzureWebJobsStorage' application setting is present.",
                functionName);

            return new ObjectResult("The API is missing its 'AzureStorageConnection' application setting.")
            {
                StatusCode = StatusCodes.Status500InternalServerError,
            };
        }

        private IActionResult Unauthenticated(string functionName)
        {
            _logger.LogWarning("Rejected {Function}: the request carries no Static Web Apps principal.", functionName);
            return new UnauthorizedResult();
        }

        [Function(nameof(GetDomains))]
        public async Task<IActionResult> GetDomains(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Domains")] HttpRequest req)
        {
            IActionResult misconfigured = CheckStorageConfigured(nameof(GetDomains));

            if (misconfigured != null)
            {
                return misconfigured;
            }

            TableClient tableClient = _tables.GetTableClient(ConfigurationTable);
            AsyncPageable<Configuration> queryResults = tableClient.QueryAsync<Configuration>(filter: "PartitionKey eq 'Domains'");
            List<Configuration> list = new List<Configuration>();

            await foreach (Configuration entity in queryResults)
            {
                list.Add(entity);
            }

            _logger.LogInformation("Returning {Count} configured domains.", list.Count);

            return new OkObjectResult(list);
        }

        [Function(nameof(GetUserLinks))]
        public async Task<IActionResult> GetUserLinks(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Links")] HttpRequest req)
        {
            IActionResult misconfigured = CheckStorageConfigured(nameof(GetUserLinks));

            if (misconfigured != null)
            {
                return misconfigured;
            }

            string userName = GetUserName(req);

            if (string.IsNullOrEmpty(userName))
            {
                return Unauthenticated(nameof(GetUserLinks));
            }

            TableClient tableClient = _tables.GetTableClient(ShortUrlTable);
            AsyncPageable<ShortUrl> queryResults = tableClient.QueryAsync<ShortUrl>(filter: $"UserPrincipleName eq '{EscapeODataLiteral(userName)}'");
            List<ShortUrl> list = new List<ShortUrl>();

            await foreach (ShortUrl entity in queryResults)
            {
                list.Add(entity);
            }

            return new OkObjectResult(list);
        }

        [Function(nameof(GetLinkUsage))]
        public async Task<IActionResult> GetLinkUsage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Links/Usage")] HttpRequest req)
        {
            req.HttpContext.Response.Headers.CacheControl = "no-store";
            string userName = GetUserName(req);
            if (string.IsNullOrEmpty(userName))
            {
                return Unauthenticated(nameof(GetLinkUsage));
            }

            try
            {
                using var timeout = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(req.HttpContext.RequestAborted);
                timeout.CancelAfter(TimeSpan.FromSeconds(30));
                TableClient tableClient = _tables.GetTableClient(ShortUrlTable);
                var links = new List<ShortUrl>();
                await foreach (ShortUrl link in tableClient.QueryAsync<ShortUrl>(
                    filter: $"UserPrincipleName eq '{EscapeODataLiteral(userName)}'",
                    select: new[] { "PartitionKey", "RowKey" },
                    cancellationToken: timeout.Token))
                {
                    links.Add(link);
                }

                return new OkObjectResult(await _usage.GetUsageAsync(links, timeout.Token));
            }
            catch (Exception)
            {
                _logger.LogWarning("Link usage is unavailable; check managed API storage and analytics configuration and workspace access.");
                return new ObjectResult("Usage is temporarily unavailable. Check the managed API storage and Analytics application settings and workspace reader access, then retry.")
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable,
                };
            }
        }

        [Function(nameof(DeleteLink))]
        public async Task<IActionResult> DeleteLink(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Links/{partitionKey}/{rowKey}")] HttpRequest req, string partitionKey, string rowKey)
        {
            IActionResult misconfigured = CheckStorageConfigured(nameof(DeleteLink));

            if (misconfigured != null)
            {
                return misconfigured;
            }

            string userName = GetUserName(req);

            if (string.IsNullOrEmpty(userName))
            {
                return Unauthenticated(nameof(DeleteLink));
            }

            TableClient tableClient = _tables.GetTableClient(ShortUrlTable);
            ShortUrl existing;

            try
            {
                existing = await tableClient.GetEntityAsync<ShortUrl>(partitionKey, rowKey);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return new NotFoundResult();
            }

            // Only the owner may delete a link. Report a miss rather than a refusal so the
            // endpoint cannot be used to probe for other users' short keys.
            if (!string.Equals(existing.UserPrincipleName, userName, StringComparison.Ordinal))
            {
                _logger.LogWarning("Rejected {Function}: the caller does not own the requested link.", nameof(DeleteLink));
                return new NotFoundResult();
            }

            await tableClient.DeleteEntityAsync(partitionKey, rowKey, existing.ETag);
            return new OkResult();
        }

        [Function(nameof(IngestShortLink))]
        public async Task<IActionResult> IngestShortLink(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Links")] HttpRequest req)
        {
            IActionResult misconfigured = CheckStorageConfigured(nameof(IngestShortLink));

            if (misconfigured != null)
            {
                return misconfigured;
            }

            string userName = GetUserName(req);

            if (string.IsNullOrEmpty(userName))
            {
                return Unauthenticated(nameof(IngestShortLink));
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            ShortUrl data;

            try
            {
                data = JsonConvert.DeserializeObject<ShortUrl>(requestBody);
            }
            catch (JsonException)
            {
                data = null;
            }

            if (data == null
                || string.IsNullOrWhiteSpace(data.PartitionKey)
                || string.IsNullOrWhiteSpace(data.RowKey)
                || !IsSafeTargetUrl(data.Url))
            {
                _logger.LogWarning("Rejected {Function}: the payload is not a valid short link.", nameof(IngestShortLink));
                return new BadRequestObjectResult("A short key, a domain and an http(s) url are required.");
            }

            data.UserPrincipleName = userName;

            // Never trust client supplied concurrency metadata for a new entity.
            data.ETag = default;
            data.Timestamp = null;

            TableClient tableClient = _tables.GetTableClient(ShortUrlTable);

            try
            {
                // Add rather than upsert so a short key can never overwrite someone else's link.
                await tableClient.AddEntityAsync(data);
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                return new ConflictObjectResult("That short link is already taken. Pick a different short key.");
            }

            return new OkResult();
        }
    }
}
