using DealProcessor.Core.Models;

namespace DealProcessor.MT5.Helpers;

public static class TradeRequestValidator
{
    public static ValidationResult ValidateBasic(TradeRequest request)
    {
        if (request.RequestId == Guid.Empty)
            return ValidationResult.Failure("RequestId is required.");

        if (string.IsNullOrWhiteSpace(request.ClientId))
            return ValidationResult.Failure("ClientId is required.");

        if (string.IsNullOrWhiteSpace(request.Symbol))
            return ValidationResult.Failure("Symbol is required.");

        if (request.Volume <= 0)
            return ValidationResult.Failure("Volume must be greater than zero.");

        if (request.StopLoss.HasValue && request.StopLoss <= 0)
            return ValidationResult.Failure("StopLoss must be greater than zero.");

        if (request.TakeProfit.HasValue && request.TakeProfit <= 0)
            return ValidationResult.Failure("TakeProfit must be greater than zero.");

        return ValidationResult.Success();
    }
}