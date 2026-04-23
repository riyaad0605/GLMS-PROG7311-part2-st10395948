using GLMS.Web.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GLMS.Tests
{
    /// <summary>
    /// Unit tests for CurrencyService.
    /// Tests the USD-to-ZAR conversion math independently of any live API call.
    /// </summary>
    public class CurrencyServiceTests
    {
        private readonly CurrencyService _service;

        public CurrencyServiceTests()
        {
            var httpClient = new HttpClient();
            var logger = new Mock<ILogger<CurrencyService>>().Object;
            _service = new CurrencyService(httpClient, logger);
        }

        // ── ConvertUsdToZar ────────────────────────────────────────────────

        [Fact]
        public void ConvertUsdToZar_CorrectAmount_ReturnsExpectedResult()
        {
            // Arrange
            decimal amountUsd = 100m;
            decimal rate = 18.50m;

            // Act
            decimal result = _service.ConvertUsdToZar(amountUsd, rate);

            // Assert
            Assert.Equal(1850.00m, result);
        }

        [Fact]
        public void ConvertUsdToZar_SmallAmount_RoundsToTwoDecimalPlaces()
        {
            // Arrange: 1 USD at rate 18.3333...
            decimal amountUsd = 1m;
            decimal rate = 18.3333m;

            // Act
            decimal result = _service.ConvertUsdToZar(amountUsd, rate);

            // Assert: result should be rounded to 2 dp
            Assert.Equal(18.33m, result);
        }

        [Fact]
        public void ConvertUsdToZar_ZeroAmount_ReturnsZero()
        {
            // Arrange
            decimal amountUsd = 0m;
            decimal rate = 18.50m;

            // Act
            decimal result = _service.ConvertUsdToZar(amountUsd, rate);

            // Assert
            Assert.Equal(0m, result);
        }

        [Fact]
        public void ConvertUsdToZar_LargeAmount_ReturnsCorrectResult()
        {
            // Arrange: simulates a large freight invoice
            decimal amountUsd = 50000m;
            decimal rate = 19.25m;

            // Act
            decimal result = _service.ConvertUsdToZar(amountUsd, rate);

            // Assert
            Assert.Equal(962500.00m, result);
        }

        [Fact]
        public void ConvertUsdToZar_NegativeAmount_ThrowsArgumentException()
        {
            // Arrange
            decimal amountUsd = -50m;
            decimal rate = 18.50m;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.ConvertUsdToZar(amountUsd, rate));

            Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ConvertUsdToZar_ZeroRate_ThrowsArgumentException()
        {
            // Arrange
            decimal amountUsd = 100m;
            decimal rate = 0m;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.ConvertUsdToZar(amountUsd, rate));

            Assert.Contains("positive", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ConvertUsdToZar_NegativeRate_ThrowsArgumentException()
        {
            // Arrange
            decimal amountUsd = 100m;
            decimal rate = -5m;

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                _service.ConvertUsdToZar(amountUsd, rate));
        }

        [Theory]
        [InlineData(10, 18.00, 180.00)]
        [InlineData(250, 19.50, 4875.00)]
        [InlineData(1000, 17.75, 17750.00)]
        [InlineData(0.99, 20.00, 19.80)]
        public void ConvertUsdToZar_MultipleScenarios_ReturnsCorrectValues(
            double usd, double rate, double expected)
        {
            // Act
            decimal result = _service.ConvertUsdToZar((decimal)usd, (decimal)rate);

            // Assert
            Assert.Equal((decimal)expected, result);
        }
    }
}
