using DealProcessor.Core.Enums;
using DealProcessor.Core.Interfaces;
using DealProcessor.Core.Models;
using Microsoft.Extensions.Logging;

namespace DealProcessor.Console.Simulation;

public sealed class ClientSimulator
{
    private readonly string _clientId;
    private readonly ITradeRequestQueue _queue;
    private readonly IRequestRegistry _requestRegistry;
    private readonly ILogger<ClientSimulator> _logger;

    public ClientSimulator(
        string clientId,
        ITradeRequestQueue queue,
        IRequestRegistry requestRegistry,
        ILogger<ClientSimulator> logger)
    {
        _clientId = clientId;
        _queue = queue;
        _requestRegistry = requestRegistry;
        _logger = logger;
    }

    public async Task RunAsync(int requestCount, CancellationToken cancellationToken)
    {
        for (var i = 0; i < requestCount; i++)
        {
            var request = BuildRandomRequest();

            if (!_requestRegistry.TryRegister(request))
            {
                _logger.LogWarning(
                    "Duplicate request detected for RequestId {RequestId}",
                    request.RequestId);
                continue;
            }

            _requestRegistry.UpdateStatus(request.RequestId, RequestStatus.Queued);

            await _queue.EnqueueAsync(request, cancellationToken);

            _logger.LogInformation(
                "Client {ClientId} queued request {RequestId} ({TradeType} {Symbol} {Volume} lots @ {PriceOrder})",
                request.ClientId,
                request.RequestId,
                request.TradeType,
                request.Symbol,
                request.Volume,
                request.PriceOrder);

            await Task.Delay(Random.Shared.Next(50, 200), cancellationToken);
        }
    }

    private TradeRequest BuildRandomRequest()
    {
        var marketData = GetRandomMarketData();
        var tradeType = Random.Shared.Next(0, 2) == 0 ? TradeType.Buy : TradeType.Sell;
        var volume = GetRandomVolume();

        var stopLoss = tradeType == TradeType.Buy
            ? marketData.Price - GetRandomDistance(marketData.Symbol)
            : marketData.Price + GetRandomDistance(marketData.Symbol);

        var takeProfit = tradeType == TradeType.Buy
            ? marketData.Price + GetRandomDistance(marketData.Symbol)
            : marketData.Price - GetRandomDistance(marketData.Symbol);

        return new TradeRequest
        {
            ClientId = _clientId,
            TradeType = tradeType,
            Symbol = marketData.Symbol,
            Volume = volume,
            PriceOrder = marketData.Price,
            Digits = marketData.Digits,
            StopLoss = RoundToDigits(stopLoss, marketData.Digits),
            TakeProfit = RoundToDigits(takeProfit, marketData.Digits)
        };
    }

    private static (string Symbol, decimal Price, int Digits) GetRandomMarketData()
    {
        var instruments = new[]
        {
            ("EURUSD", 1.08452m, 5),
            ("GBPUSD", 1.27134m, 5),
            ("USDJPY", 149.235m, 3),
            ("XAUUSD", 2178.45m, 2)
        };

        return instruments[Random.Shared.Next(instruments.Length)];
    }

    private static decimal GetRandomVolume()
    {
        var volumes = new[] { 0.01m, 0.05m, 0.10m, 0.20m, 0.50m, 1.00m };
        return volumes[Random.Shared.Next(volumes.Length)];
    }

    private static decimal GetRandomDistance(string symbol)
    {
        return symbol switch
        {
            "EURUSD" => 0.00100m,
            "GBPUSD" => 0.00150m,
            "USDJPY" => 0.100m,
            "XAUUSD" => 5.00m,
            _ => 0.00100m
        };
    }

    private static decimal RoundToDigits(decimal value, int digits)
        => Math.Round(value, digits, MidpointRounding.AwayFromZero);
}