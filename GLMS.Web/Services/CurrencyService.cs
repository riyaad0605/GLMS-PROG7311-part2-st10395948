using System.Text.Json;

namespace GLMS.Web.Services
{
    public interface ICurrencyService
    {
        Task<decimal> GetUsdToZarRateAsync();
        decimal ConvertUsdToZar(decimal amountUsd, decimal exchangeRate);
    }

    public class CurrencyService : ICurrencyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CurrencyService> _logger;
        private const decimal FallbackRate = 18.50m;

        public CurrencyService(HttpClient httpClient, ILogger<CurrencyService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<decimal> GetUsdToZarRateAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://open.er-api.com/v6/latest/USD");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("rates", out var rates) &&
                    rates.TryGetProperty("ZAR", out var zarRate))
                {
                    return zarRate.GetDecimal();
                }

                _logger.LogWarning("ZAR rate not found in API response. Using fallback.");
                return FallbackRate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Currency API failed. Using fallback rate {Rate}.", FallbackRate);
                return FallbackRate;
            }
        }

        public decimal ConvertUsdToZar(decimal amountUsd, decimal exchangeRate)
        {
            if (amountUsd < 0)
                throw new ArgumentException("Amount cannot be negative.", nameof(amountUsd));

            if (exchangeRate <= 0)
                throw new ArgumentException("Exchange rate must be positive.", nameof(exchangeRate));

            return Math.Round(amountUsd * exchangeRate, 2);
        }
    }
}
