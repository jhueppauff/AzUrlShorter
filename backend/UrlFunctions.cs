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
        private readonly ILogger<UrlFunctions> _logger;

        public UrlFunctions(ILogger<UrlFunctions> logger)
        {
            _logger = logger;
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

            _logger.LogInformation("Returning {Count} configured domains.", list.Count);

            return new OkObjectResult(list);
        }

        [Function(nameof(GetUserLinks))]
        public async Task<IActionResult> GetUserLinks(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Links")] HttpRequest req,
            [TableInput("shorturls", Connection = "AzureStorageConnection")] TableClient tableClient)
        {
            #region Null Checks
            if (tableClient == null)
            {
                throw new ArgumentNullException(nameof(tableClient));
            }
            #endregion

            string userName = GetUserName(req);

            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("Rejected {Function}: the request carries no Static Web Apps principal.", nameof(GetUserLinks));
                return new UnauthorizedResult();
            }

            AsyncPageable<ShortUrl> queryResults = tableClient.QueryAsync<ShortUrl>(filter: $"UserPrincipleName eq '{EscapeODataLiteral(userName)}'");

            List<ShortUrl> list = new List<ShortUrl>();

            await foreach (ShortUrl entity in queryResults)
            {
                list.Add(entity);
            }

            return new OkObjectResult(list);
        }

        [Function(nameof(DeleteLink))]
        public async Task<IActionResult> DeleteLink(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Links/{partitionKey}/{rowKey}")] HttpRequest req, string partitionKey, string rowKey,
            [TableInput("shorturls", Connection = "AzureStorageConnection")] TableClient tableClient)
        {
            #region Null Checks
            if (tableClient == null)
            {
                throw new ArgumentNullException(nameof(tableClient));
            }
            #endregion

            string userName = GetUserName(req);

            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("Rejected {Function}: the request carries no Static Web Apps principal.", nameof(DeleteLink));
                return new UnauthorizedResult();
            }

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
        public async Task<IngestShortLinkOutput> IngestShortLink([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Links")] HttpRequest req)
        {
            string userName = GetUserName(req);

            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("Rejected {Function}: the request carries no Static Web Apps principal.", nameof(IngestShortLink));
                return new IngestShortLinkOutput { Result = new UnauthorizedResult() };
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
                return new IngestShortLinkOutput
                {
                    Result = new BadRequestObjectResult("partitionKey, rowKey and an http(s) url are required."),
                };
            }

            data.UserPrincipleName = userName;

            // Never trust client supplied concurrency metadata for a new entity.
            data.ETag = default;
            data.Timestamp = null;

            return new IngestShortLinkOutput { Entity = data, Result = new OkResult() };
        }
    }
}
