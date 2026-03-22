using DealProcessor.Console.Helpers;
using DealProcessor.Core.Common;
using DealProcessor.Core.Enums;
using DealProcessor.Core.Interfaces;
using DealProcessor.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DealProcessor.Console.HostedServices;

public sealed class DealProcessorService : BackgroundService
{
    private readonly ITradeRequestQueue _queue;
    private readonly IRequestRegistry _requestRegistry;
    private readonly IExecutionResultStore _resultStore;
    private readonly IMtTradeGateway _mtTradeGateway;
    private readonly RetryOptions _retryOptions;
    private readonly ILogger<DealProcessorService> _logger;

    public DealProcessorService(
        ITradeRequestQueue queue,
        IRequestRegistry requestRegistry,
        IExecutionResultStore resultStore,
        IMtTradeGateway mtTradeGateway,
        IOptions<RetryOptions> retryOptions,
        ILogger<DealProcessorService> logger)
    {
        _queue = queue;
        _requestRegistry = requestRegistry;
        _resultStore = resultStore;
        _mtTradeGateway = mtTradeGateway;
        _retryOptions = retryOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Deal Processor started.");

        await _mtTradeGateway.ConnectAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            TradeRequest request;

            try
            {
                request = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while dequeuing trade request.");
                continue;
            }

            try
            {
                _requestRegistry.UpdateStatus(request.RequestId, RequestStatus.Validating);

                _logger.LogInformation(
                    "Validating request {RequestId} from client {ClientId}. RetryCount={RetryCount}",
                    request.RequestId,
                    request.ClientId,
                    request.RetryCount);

                var validation = await _mtTradeGateway.ValidateAsync(request, stoppingToken);

                if (!validation.IsValid)
                {
                    var failedValidationResult = new TradeExecutionResult
                    {
                        RequestId = request.RequestId,
                        ClientId = request.ClientId,
                        Success = false,
                        Message = validation.ErrorMessage ?? "Validation failed.",
                        Status = RequestStatus.Rejected,
                        MtRetCode = "VALIDATION_FAILED"
                    };

                    _requestRegistry.Complete(request.RequestId, failedValidationResult);
                    _resultStore.Save(failedValidationResult);

                    _logger.LogWarning(
                        "Request {RequestId} rejected. Reason: {Reason}",
                        request.RequestId,
                        failedValidationResult.Message);

                    continue;
                }

                _requestRegistry.UpdateStatus(request.RequestId, RequestStatus.Executing);

                _logger.LogInformation(
                    "Executing request {RequestId} for symbol {Symbol}, volume {Volume}. RetryCount={RetryCount}",
                    request.RequestId,
                    request.Symbol,
                    request.Volume,
                    request.RetryCount);

                var executionResult = await _mtTradeGateway.ExecuteAsync(request, stoppingToken);

                _requestRegistry.Complete(request.RequestId, executionResult);
                _resultStore.Save(executionResult);

                if (executionResult.Success)
                {
                    _logger.LogInformation(
                        "Request {RequestId} executed successfully. Ticket={Ticket}, MtRequestId={MtRequestId}",
                        request.RequestId,
                        executionResult.MtTicketId,
                        executionResult.MtRequestId);
                }
                else
                {
                    _logger.LogWarning(
                        "Request {RequestId} execution failed. RetCode={RetCode}, Message={Message}, RetryCount={RetryCount}",
                        request.RequestId,
                        executionResult.MtRetCode,
                        executionResult.Message,
                        request.RetryCount);

                    if (ShouldRetry(request, executionResult))
                    {
                        var retryRequest = CreateRetryRequest(request);

                        _requestRegistry.UpdateStatus(retryRequest.RequestId, RequestStatus.Queued);

                        _logger.LogWarning(
                            "Retrying request {RequestId}. NextRetryCount={RetryCount} after {DelayMs} ms.",
                            retryRequest.RequestId,
                            retryRequest.RetryCount,
                            _retryOptions.DelayMs);

                        await Task.Delay(_retryOptions.DelayMs, stoppingToken);
                        await _queue.EnqueueAsync(retryRequest, stoppingToken);

                        continue;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                var cancelledResult = new TradeExecutionResult
                {
                    RequestId = request.RequestId,
                    ClientId = request.ClientId,
                    Success = false,
                    Message = "Request cancelled because application is shutting down.",
                    Status = RequestStatus.Failed,
                    MtRetCode = "CANCELLED"
                };

                _requestRegistry.Complete(request.RequestId, cancelledResult);
                _resultStore.Save(cancelledResult);

                _logger.LogWarning(
                    "Request {RequestId} was cancelled because the application is shutting down.",
                    request.RequestId);
            }
            catch (Exception ex)
            {
                var failedResult = new TradeExecutionResult
                {
                    RequestId = request.RequestId,
                    ClientId = request.ClientId,
                    Success = false,
                    Message = ex.Message,
                    Status = RequestStatus.Failed,
                    MtRetCode = "EXCEPTION"
                };

                _requestRegistry.Complete(request.RequestId, failedResult);
                _resultStore.Save(failedResult);

                _logger.LogError(ex, "Unhandled error while processing request {RequestId}", request.RequestId);

                if (ShouldRetry(request, failedResult))
                {
                    try
                    {
                        var retryRequest = CreateRetryRequest(request);

                        _requestRegistry.UpdateStatus(retryRequest.RequestId, RequestStatus.Queued);

                        _logger.LogWarning(
                            "Retrying request {RequestId} after exception. NextRetryCount={RetryCount} after {DelayMs} ms.",
                            retryRequest.RequestId,
                            retryRequest.RetryCount,
                            _retryOptions.DelayMs);

                        await Task.Delay(_retryOptions.DelayMs, stoppingToken);
                        await _queue.EnqueueAsync(retryRequest, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogWarning(
                            "Retry for request {RequestId} skipped because the application is shutting down.",
                            request.RequestId);
                    }
                }
            }
        }

        _logger.LogInformation("Deal Processor stopped.");
    }

    private bool ShouldRetry(TradeRequest request, TradeExecutionResult result)
    {
        if (!_retryOptions.Enabled)
            return false;

        if (request.RetryCount >= _retryOptions.MaxRetryAttempts)
            return false;

        return RetryPolicyHelper.ShouldRetry(result);
    }

    private static TradeRequest CreateRetryRequest(TradeRequest request)
    {
        return new TradeRequest
        {
            RequestId = request.RequestId,
            ClientId = request.ClientId,
            TradeType = request.TradeType,
            Symbol = request.Symbol,
            Volume = request.Volume,
            PriceOrder = request.PriceOrder,
            Digits = request.Digits,
            StopLoss = request.StopLoss,
            TakeProfit = request.TakeProfit,
            RetryCount = request.RetryCount + 1,
            CreatedAtUtc = request.CreatedAtUtc
        };
    }
}