using DealProcessor.Console.HostedServices;
using DealProcessor.Console.Simulation;
using DealProcessor.Core.Common;
using DealProcessor.Core.Interfaces;
using DealProcessor.Infrastructure.Queues;
using DealProcessor.Infrastructure.Stores;
using DealProcessor.Infrastructure.Tracking;
using DealProcessor.MT5.Gateways;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System.Net;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/deal-processor-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.Configure<Mt5Options>(
    builder.Configuration.GetSection("Mt5Options"));

builder.Services.Configure<SimulationOptions>(
    builder.Configuration.GetSection("SimulationOptions"));

builder.Services.AddSingleton<ITradeRequestQueue, TradeRequestQueue>();
builder.Services.AddSingleton<IRequestRegistry, InMemoryRequestRegistry>();
builder.Services.AddSingleton<IExecutionResultStore, InMemoryExecutionResultStore>();
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
});
builder.Services.AddHttpClient<IMtTradeGateway, Mt5TradeGateway>()
    .ConfigurePrimaryHttpMessageHandler(() =>
        new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer()
        });

builder.Services.AddHostedService<DealProcessorService>();
builder.Services.Configure<RetryOptions>(
builder.Configuration.GetSection("RetryOptions"));

var host = builder.Build();

await host.StartAsync();

var queue = host.Services.GetRequiredService<ITradeRequestQueue>();
var registry = host.Services.GetRequiredService<IRequestRegistry>();
var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
var simulationOptions = host.Services
.GetRequiredService<IOptions<SimulationOptions>>()
.Value;


var clients = Enumerable.Range(1, simulationOptions.ClientCount)
.Select(i => new ClientSimulator(
    clientId: $"Client-{i}",
    queue: queue,
    requestRegistry: registry,
    logger: loggerFactory.CreateLogger<ClientSimulator>()))
.ToList();

using var cts = new CancellationTokenSource();

var tasks = clients
.Select(c => c.RunAsync(simulationOptions.RequestsPerClient, cts.Token))
.ToArray();

await Task.WhenAll(tasks);

Log.Information("All clients finished sending requests. Waiting for queue to drain...");

while (queue.Count > 0 && !lifetime.ApplicationStopping.IsCancellationRequested)
{
    Log.Information("Pending queued requests: {Count}", queue.Count);
    await Task.Delay(1000);
}

Log.Information("Queue drained. Stopping host...");
await host.StopAsync();
