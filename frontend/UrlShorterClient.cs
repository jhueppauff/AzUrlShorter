using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using frontend.Model;

namespace frontend
{
    public class UrlShorterClient
    {
        private readonly HttpClient client;

        public UrlShorterClient(IHttpClientFactory factory)
        {
            client = factory.CreateClient(nameof(UrlShorterClient));
        }

        public async Task<HttpResponseMessage> CreateShorterUrl(Model.ShortUrl shortUrl)
        {
            return await client.PostAsJsonAsync($"/api/Links", shortUrl).ConfigureAwait(false);
        }

        public async Task<List<Model.ConfigurationEntry>> GetDomains()
        {
            return await client.GetFromJsonAsync<List<ConfigurationEntry>>("/api/Domains").ConfigureAwait(false);
        }

        public async Task<List<ShortUrl>> GetLinks()
        {
            return await client.GetFromJsonAsync<List<ShortUrl>>("/api/Links").ConfigureAwait(false);
        }

        public async Task<HttpResponseMessage> DeleteLink(ShortUrl shortUrl)
        {
            return await client.DeleteAsync($"/api/Links{shortUrl.PartitionKey}/{shortUrl.RowKey}").ConfigureAwait(false);
        }
    }
}