namespace ECommerce.Shared.Core.Retry;

public static class ExponentialBackoffCalculator
{
    public static int GetDelaySeconds(
        int retryCount,
        int initialDelaySeconds,
        int maxDelaySeconds)
    {
        if (retryCount <= 0)
        {
            return initialDelaySeconds;
        }

        var delay = (int)Math.Pow(2, retryCount - 1) * initialDelaySeconds;

        return Math.Min(maxDelaySeconds, delay);
    }
}