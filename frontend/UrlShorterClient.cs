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
            return await client.PostAsJsonAsync($"/api/Ingest", shortUrl).ConfigureAwait(false);
        }

        public async Task<List<Model.ConfigurationEntry>> GetDomains()
        {
            return await client.GetFromJsonAsync<List<ConfigurationEntry>>("/api/GetDomains").ConfigureAwait(false);
        }
    }
}