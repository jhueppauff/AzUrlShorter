using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using backend.Model;
using Microsoft.Extensions.Configuration;

namespace Shorter.Backend
{
    public sealed class LinkUsage
    {
        [JsonPropertyName("partitionKey")]
        public string PartitionKey { get; init; }

        [JsonPropertyName("rowKey")]
        public string RowKey { get; init; }

        [JsonPropertyName("uses")]
        public long Uses { get; init; }

        [JsonPropertyName("lastUsed")]
        public string LastUsed { get; init; }
    }

    public sealed class LinkUsageProvider : IDisposable
    {
        private readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        private readonly string _workspaceId;
        private readonly string _tenantId;
        private readonly string _clientId;
        private readonly string _clientSecret;

        public LinkUsageProvider(IConfiguration configuration)
        {
            _workspaceId = Setting(configuration, "AnalyticsWorkspaceId");
            _tenantId = Setting(configuration, "AnalyticsTenantId");
            _clientId = Setting(configuration, "AnalyticsClientId");
            _clientSecret = Setting(configuration, "AnalyticsClientSecret");
        }

        private static string Setting(IConfiguration configuration, string name) =>
            configuration[name] ?? configuration[$"Values:{name}"];

        public async Task<IReadOnlyList<LinkUsage>> GetUsageAsync(
            IReadOnlyList<ShortUrl> links, CancellationToken cancellationToken)
        {
            if (links.Count == 0)
            {
                return Array.Empty<LinkUsage>();
            }

            if (!Guid.TryParse(_workspaceId, out Guid workspaceId)
                || !Guid.TryParse(_tenantId, out Guid tenantId)
                || !Guid.TryParse(_clientId, out Guid clientId)
                || string.IsNullOrWhiteSpace(_clientSecret))
            {
                throw new InvalidOperationException("Analytics is not configured.");
            }

            var owned = links.Select(link => (link.PartitionKey, link.RowKey)).ToHashSet();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(25));
            using var tokenRequest = new HttpRequestMessage(
                HttpMethod.Post, $"https://login.microsoftonline.com/{tenantId:D}/oauth2/v2.0/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = clientId.ToString("D"),
                    ["client_secret"] = _clientSecret,
                    ["scope"] = "https://api.loganalytics.io/.default",
                }),
            };
            using var tokenResponse = await _http.SendAsync(tokenRequest, timeout.Token);
            tokenResponse.EnsureSuccessStatusCode();
            using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(timeout.Token));
            string accessToken = tokenJson.RootElement.GetProperty("access_token").GetString();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new InvalidOperationException("Analytics authentication failed.");
            }

            // JSON string literals safely escape quotes, backslashes and control characters
            // in stored keys, including links created through a crafted API request.
            string pairs = string.Join(",\n", owned.Select(pair =>
                $"{JsonSerializer.Serialize(pair.PartitionKey)}, {JsonSerializer.Serialize(pair.RowKey)}"));
            string query = $"""
                let OwnedLinks = datatable(shortKey:string, domain:string) [
                {pairs}
                ];
                AppEvents
                | where TimeGenerated >= ago(30d) and TimeGenerated <= now()
                | where Name == "ShortLinkUsed"
                | extend shortKey = tostring(Properties.shortKey), domain = tostring(Properties.domain)
                | join kind=inner (OwnedLinks) on shortKey, domain
                | summarize uses = sum(ItemCount), lastUsed = max(TimeGenerated) by shortKey, domain
                | project shortKey, domain, uses, lastUsed
                """;
            using var queryRequest = new HttpRequestMessage(
                HttpMethod.Post, $"https://api.loganalytics.azure.com/v1/workspaces/{workspaceId:D}/query")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { query, timespan = "P30D" }), Encoding.UTF8, "application/json"),
            };
            queryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var queryResponse = await _http.SendAsync(queryRequest, timeout.Token);
            queryResponse.EnsureSuccessStatusCode();
            using var result = JsonDocument.Parse(await queryResponse.Content.ReadAsStringAsync(timeout.Token));
            JsonElement root = result.RootElement;
            // Log Analytics can return HTTP 200 with a partial query failure.
            if (root.TryGetProperty("error", out _))
            {
                throw new InvalidOperationException("Analytics returned an error.");
            }

            JsonElement tables = root.GetProperty("tables");
            if (tables.GetArrayLength() != 1
                || tables[0].GetProperty("name").GetString() != "PrimaryResult")
            {
                throw new InvalidOperationException("Unexpected analytics response.");
            }

            JsonElement table = tables[0];
            string[] expectedColumns = { "shortKey", "domain", "uses", "lastUsed" };
            JsonElement columns = table.GetProperty("columns");
            if (columns.GetArrayLength() != expectedColumns.Length
                || columns.EnumerateArray().Where((column, index) =>
                    column.GetProperty("name").GetString() != expectedColumns[index]).Any())
            {
                throw new InvalidOperationException("Unexpected analytics columns.");
            }

            var usage = new Dictionary<(string, string), LinkUsage>();
            foreach (JsonElement row in table.GetProperty("rows").EnumerateArray())
            {
                if (row.GetArrayLength() != 4)
                {
                    throw new InvalidOperationException("Unexpected analytics row.");
                }

                var key = (row[0].GetString(), row[1].GetString());
                if (!owned.Contains(key)
                    || !row[2].TryGetInt64(out long uses) || uses < 0
                    || !DateTimeOffset.TryParse(row[3].GetString(), CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal, out DateTimeOffset lastUsed))
                {
                    throw new InvalidOperationException("Invalid analytics row.");
                }

                usage.Add(key, new LinkUsage
                {
                    PartitionKey = key.Item1,
                    RowKey = key.Item2,
                    Uses = uses,
                    LastUsed = lastUsed.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                });
            }

            // Only a complete, successful query may supply zero counts for absent events.
            return owned.Select(key => usage.TryGetValue(key, out LinkUsage value) ? value : new LinkUsage
            {
                PartitionKey = key.PartitionKey,
                RowKey = key.RowKey,
                Uses = 0,
                LastUsed = null,
            }).ToArray();
        }

        public void Dispose() => _http.Dispose();
    }
}
