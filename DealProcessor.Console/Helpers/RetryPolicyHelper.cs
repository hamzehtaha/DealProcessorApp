using DealProcessor.Core.Models;

namespace DealProcessor.Console.Helpers;

public static class RetryPolicyHelper
{
    public static bool ShouldRetry(TradeExecutionResult result)
    {
        if (result.Success)
            return false;

        if (string.IsNullOrWhiteSpace(result.MtRetCode))
            return false;

        bool s=  result.MtRetCode.Equals("TIMEOUT", StringComparison.OrdinalIgnoreCase)
            || result.MtRetCode.Equals("HTTP_500", StringComparison.OrdinalIgnoreCase)
            || result.MtRetCode.Equals("PRICE_CHANGED", StringComparison.OrdinalIgnoreCase)
            || result.MtRetCode.Equals("CONNECTION_ERROR", StringComparison.OrdinalIgnoreCase)
            || result.MtRetCode.Equals("MARKET_CLOSED", StringComparison.OrdinalIgnoreCase)
            || result.MtRetCode.Equals("REQUEST_FAILED", StringComparison.OrdinalIgnoreCase);
        return s;
    }
}