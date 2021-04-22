using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AzureStaticWebApps.Blazor.Authentication;

namespace frontend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services
                .AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
                .AddStaticWebAppsAuthentication();

            string endpoint = builder.Configuration.GetSection("Backend").GetValue<string>("Endpoint");

            builder.Services.AddHttpClient(nameof(UrlShorterClient),
                client =>
                {
                    client.BaseAddress = new Uri(endpoint);

                });

            builder.Services.AddTransient<UrlShorterClient>();

            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(UrlShorterClient)));

            await builder.Build().RunAsync();
        }
    }
}
