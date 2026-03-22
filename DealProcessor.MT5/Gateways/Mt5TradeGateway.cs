using System.Diagnostics;
using System.Net.Http.Json;
using DealProcessor.Core.Common;
using DealProcessor.Core.Enums;
using DealProcessor.Core.Interfaces;
using DealProcessor.Core.Models;
using DealProcessor.MT5.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DealProcessor.MT5.Gateways;

public sealed class Mt5TradeGateway : IMtTradeGateway
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Mt5TradeGateway> _logger;
    private readonly Mt5Options _options;
    private volatile bool _isConnected;

    public Mt5TradeGateway(
        HttpClient httpClient,
        ILogger<Mt5TradeGateway> logger,
        IOptions<Mt5Options> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        }
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (_isConnected)
            return;

        const int maxAttempts = 5;
        const int delayMs = 2000;

        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _logger.LogInformation(
                    "Connecting to MT5 gateway. Attempt {Attempt} of {MaxAttempts}. UseRealMt5Api={UseRealMt5Api}",
                    attempt,
                    maxAttempts,
                    _options.UseRealMt5Api);

                if (_options.UseRealMt5Api)
                {
                    await AuthenticateAsync(cancellationToken);
                    _logger.LogInformation("MT5 Web API connection established successfully.");
                }
                else
                {
                    _logger.LogInformation("Mock MT5 mode enabled. No real MT5 authentication will be performed.");
                    await Task.Delay(100, cancellationToken);
                }

                _isConnected = true;
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;

                _logger.LogWarning(
                    ex,
                    "MT5 connection attempt {Attempt} of {MaxAttempts} failed.",
                    attempt,
                    maxAttempts);

                if (attempt == maxAttempts)
                    break;

                _logger.LogInformation(
                    "Waiting {DelayMs} ms before next MT5 connection attempt.",
                    delayMs);

                await Task.Delay(delayMs, cancellationToken);
            }
        }

        _isConnected = false;

        throw new InvalidOperationException(
            $"Failed to connect to MT5 gateway after {maxAttempts} attempts.",
            lastException);
    }

    public Task<bool> IsConnectedAsync(CancellationToken cancellationToken)
        => Task.FromResult(_isConnected);

    public Task<ValidationResult> ValidateAsync(TradeRequest request, CancellationToken cancellationToken)
    {
        var basicValidation = TradeRequestValidator.ValidateBasic(request);
        if (!basicValidation.IsValid)
            return Task.FromResult(basicValidation);

        if (!_isConnected)
            return Task.FromResult(ValidationResult.Failure("MT5 gateway is not initialized."));

        var supportedSymbols = new[] { "EURUSD", "GBPUSD", "USDJPY", "XAUUSD" };
        if (!supportedSymbols.Contains(request.Symbol, StringComparer.OrdinalIgnoreCase))
            return Task.FromResult(ValidationResult.Failure($"Symbol '{request.Symbol}' is not supported."));

        if (request.Volume < 0.01m)
            return Task.FromResult(ValidationResult.Failure("Volume must be at least 0.01 lots."));

        if (request.Volume > 10m)
            return Task.FromResult(ValidationResult.Failure("Volume exceeds maximum allowed value."));

        if (!_options.UseRealMt5Api && request.Symbol.Equals("XAUUSD", StringComparison.OrdinalIgnoreCase) && request.Volume > 2m)
            return Task.FromResult(ValidationResult.Failure("Insufficient margin for XAUUSD in mock mode."));

        return Task.FromResult(ValidationResult.Success());
    }

    public async Task<TradeExecutionResult> ExecuteAsync(TradeRequest request, CancellationToken cancellationToken)
    {
        if (!_isConnected)
        {
            return new TradeExecutionResult
            {
                RequestId = request.RequestId,
                ClientId = request.ClientId,
                Success = false,
                Message = "MT5 gateway is not initialized.",
                Status = RequestStatus.Failed,
                MtRetCode = "NOT_CONNECTED"
            };
        }

        return _options.UseRealMt5Api
            ? await ExecuteRealAsync(request, cancellationToken)
            : await ExecuteMockAsync(request, cancellationToken);
    }

    private async Task<TradeExecutionResult> ExecuteMockAsync(TradeRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Mock execution. RequestId={RequestId}, ClientId={ClientId}, Symbol={Symbol}, Volume={Volume}",
            request.RequestId,
            request.ClientId,
            request.Symbol,
            request.Volume);

        await Task.Delay(1000, cancellationToken);

        var chance = Random.Shared.Next(1, 101);

        if (chance <= 10)
        {
            return new TradeExecutionResult
            {
                RequestId = request.RequestId,
                ClientId = request.ClientId,
                Success = false,
                Message = "Mock rejection: market closed.",
                Status = RequestStatus.Failed,
                MtRetCode = "MARKET_CLOSED"
            };
        }

        if (chance <= 20)
        {
            return new TradeExecutionResult
            {
                RequestId = request.RequestId,
                ClientId = request.ClientId,
                Success = false,
                Message = "Mock rejection: no money.",
                Status = RequestStatus.Failed,
                MtRetCode = "NO_MONEY"
            };
        }

        var fakeTicket = Random.Shared.NextInt64(100000000, 999999999);

        
        return new TradeExecutionResult
        {
            RequestId = request.RequestId,
            ClientId = request.ClientId,
            Success = true,
            Message = "Mock trade executed successfully.",
            MtTicketId = fakeTicket,
            Status = RequestStatus.Succeeded,
            MtRetCode = "0 Done"
        };
    }

    private async Task<TradeExecutionResult> ExecuteRealAsync(TradeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var mtRequest = BuildDealerRequest(request);

            _logger.LogInformation(
                "Sending MT5 dealer request. RequestId={RequestId}, ClientId={ClientId}, Symbol={Symbol}, Volume={Volume}",
                request.RequestId,
                request.ClientId,
                request.Symbol,
                request.Volume);

            var sendResponse = await _httpClient.PostAsJsonAsync(
                "/api/dealer/send_request",
                mtRequest,
                cancellationToken);

            if (!sendResponse.IsSuccessStatusCode)
            {
                var body = await sendResponse.Content.ReadAsStringAsync(cancellationToken);

                return new TradeExecutionResult
                {
                    RequestId = request.RequestId,
                    ClientId = request.ClientId,
                    Success = false,
                    Message = $"HTTP error from MT5 send_request: {(int)sendResponse.StatusCode} - {body}",
                    Status = RequestStatus.Failed,
                    MtRetCode = $"HTTP_{(int)sendResponse.StatusCode}"
                };
            }

            var sendResult = await sendResponse.Content.ReadFromJsonAsync<MtDealerSendRequestResponse>(cancellationToken: cancellationToken);

            if (sendResult == null)
            {
                return new TradeExecutionResult
                {
                    RequestId = request.RequestId,
                    ClientId = request.ClientId,
                    Success = false,
                    Message = "MT5 send_request returned empty response.",
                    Status = RequestStatus.Failed,
                    MtRetCode = "EMPTY_RESPONSE"
                };
            }

            if (!sendResult.Retcode.StartsWith("0"))
            {
                return new TradeExecutionResult
                {
                    RequestId = request.RequestId,
                    ClientId = request.ClientId,
                    Success = false,
                    Message = $"MT5 send_request rejected: {sendResult.Retcode}",
                    Status = RequestStatus.Failed,
                    MtRetCode = sendResult.Retcode
                };
            }

            var mtRequestId = sendResult.Answer?.ID;
            if (string.IsNullOrWhiteSpace(mtRequestId))
            {
                return new TradeExecutionResult
                {
                    RequestId = request.RequestId,
                    ClientId = request.ClientId,
                    Success = false,
                    Message = "MT5 send_request succeeded but no request ID was returned.",
                    Status = RequestStatus.Failed,
                    MtRetCode = "MISSING_REQUEST_ID"
                };
            }

            _logger.LogInformation(
                "MT5 request accepted and queued. RequestId={RequestId}, MtRequestId={MtRequestId}",
                request.RequestId,
                mtRequestId);

            return await PollRequestResultAsync(request, mtRequestId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new TradeExecutionResult
            {
                RequestId = request.RequestId,
                ClientId = request.ClientId,
                Success = false,
                Message = "Trade execution canceled.",
                Status = RequestStatus.Failed,
                MtRetCode = "CANCELLED"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in MT5 ExecuteAsync for RequestId={RequestId}", request.RequestId);

            return new TradeExecutionResult
            {
                RequestId = request.RequestId,
                ClientId = request.ClientId,
                Success = false,
                Message = ex.Message,
                Status = RequestStatus.Failed,
                MtRetCode = "EXCEPTION"
            };
        }
    }

    private async Task<TradeExecutionResult> PollRequestResultAsync(
        TradeRequest request,
        string mtRequestId,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < _options.PollTimeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(_options.PollIntervalMs, cancellationToken);

            var response = await _httpClient.GetAsync(
                $"/api/dealer/get_request_result?id={mtRequestId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                continue;

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "Received MT5 request result response. RequestId={RequestId}, MtRequestId={MtRequestId}, Body={Body}",
                request.RequestId,
                mtRequestId,
                raw);

            if (raw.Contains("done", StringComparison.OrdinalIgnoreCase) ||
                raw.Contains("\"retcode\":\"0", StringComparison.OrdinalIgnoreCase) ||
                raw.Contains("\"retcode\": \"0", StringComparison.OrdinalIgnoreCase))
            {
                return new TradeExecutionResult
                {
                    RequestId = request.RequestId,
                    ClientId = request.ClientId,
                    Success = true,
                    Message = $"Trade executed successfully through MT5 Web API. MtRequestId={mtRequestId}",
                    Status = RequestStatus.Succeeded,
                    MtRetCode = "0 Done"
                };
            }

            if (raw.Contains("reject", StringComparison.OrdinalIgnoreCase) ||
                raw.Contains("error", StringComparison.OrdinalIgnoreCase))
            {
                return new TradeExecutionResult
                {
                    RequestId = request.RequestId,
                    ClientId = request.ClientId,
                    Success = false,
                    Message = $"MT5 trade request failed. MtRequestId={mtRequestId}. Response={raw}",
                    Status = RequestStatus.Failed,
                    MtRetCode = "REQUEST_FAILED"
                };
            }
        }

        return new TradeExecutionResult
        {
            RequestId = request.RequestId,
            ClientId = request.ClientId,
            Success = false,
            Message = $"Timed out waiting for MT5 execution result. MtRequestId={mtRequestId}",
            Status = RequestStatus.Failed,
            MtRetCode = "TIMEOUT"
        };
    }

    private MtDealerTradeRequestDto BuildDealerRequest(TradeRequest request)
    {
        var type = request.TradeType == TradeType.Buy ? "0" : "1";

        return new MtDealerTradeRequestDto
        {
            Action = "200",
            Login = _options.CentralTradeLogin.ToString(),
            Symbol = request.Symbol.ToUpperInvariant(),
            Volume = ConvertLotsToMtVolume(request.Volume).ToString(),
            TypeFill = "0",
            Type = type,
            PriceOrder = request.PriceOrder?.ToString() ?? "0",
            Digits = (request.Digits ?? 5).ToString(),
            SL = request.StopLoss?.ToString(),
            TP = request.TakeProfit?.ToString()
        };
    }

    private static int ConvertLotsToMtVolume(decimal lots)
    {
        return (int)(lots * 10000);
    }

    public async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting MT5 Web API authentication...");

            var startUrl = $"/api/auth/start?version=484&agent=DealProcessor&login={_options.ManagerLogin}&type=manager";
            var startResponse = await _httpClient.GetAsync(startUrl, cancellationToken);
            startResponse.EnsureSuccessStatusCode();

            var startData = await startResponse.Content.ReadFromJsonAsync<MtAuthStartResponse>(cancellationToken: cancellationToken);

            if (startData == null || !startData.Retcode.StartsWith("0"))
                throw new Exception("MT5 auth start failed");

            var srvRand = startData.Srv_Rand;

            var passwordHash = Mt5AuthHelper.CalculatePasswordHash(_options.Password);
            var srvRandAnswer = Mt5AuthHelper.CalculateSrvRandAnswer(passwordHash, srvRand);
            var cliRand = Mt5AuthHelper.GenerateCliRand();

            var answerUrl = $"/api/auth/answer?srv_rand_answer={srvRandAnswer}&cli_rand={cliRand}";

            var answerResponse = await _httpClient.GetAsync(answerUrl, cancellationToken);
            answerResponse.EnsureSuccessStatusCode();

            var answerJson = await answerResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!answerJson.Contains("0 Done"))
                throw new Exception("MT5 authentication failed");

            _logger.LogInformation("MT5 Web API authenticated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MT5 authentication error: {Message}", ex.Message);
            throw;
        }
    }
}