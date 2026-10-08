using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shorter.Backend;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<TableClientProvider>();
        services.AddSingleton<LinkUsageProvider>();
    })
    .Build();

host.Run();
