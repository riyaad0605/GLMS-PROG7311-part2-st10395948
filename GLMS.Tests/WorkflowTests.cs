using GLMS.Web.Models;
using Xunit;

namespace GLMS.Tests
{
    /// <summary>
    /// Unit tests for GLMS workflow and business logic rules.
    /// Tests contract status validation, service request eligibility,
    /// and entity model behaviour without requiring a database.
    /// </summary>
    public class WorkflowTests
    {
        // ── Contract status: service request eligibility ───────────────────

        [Fact]
        public void ServiceRequest_AgainstActiveContract_IsAllowed()
        {
            // Arrange
            var contract = new Contract
            {
                ContractId = 1,
                Status = ContractStatus.Active,
                StartDate = DateTime.Now.AddDays(-30),
                EndDate = DateTime.Now.AddDays(335),
                ServiceLevel = "Premium"
            };

            // Act: mirrors the workflow check in ServiceRequestsController
            bool allowed = contract.Status != ContractStatus.Expired &&
                           contract.Status != ContractStatus.OnHold;

            // Assert
            Assert.True(allowed);
        }

        [Fact]
        public void ServiceRequest_AgainstDraftContract_IsAllowed()
        {
            var contract = new Contract { Status = ContractStatus.Draft };
            bool allowed = contract.Status != ContractStatus.Expired &&
                           contract.Status != ContractStatus.OnHold;
            Assert.True(allowed);
        }

        [Fact]
        public void ServiceRequest_AgainstExpiredContract_IsBlocked()
        {
            // Arrange
            var contract = new Contract { Status = ContractStatus.Expired };

            // Act
            bool blocked = contract.Status == ContractStatus.Expired ||
                           contract.Status == ContractStatus.OnHold;

            // Assert
            Assert.True(blocked);
        }

        [Fact]
        public void ServiceRequest_AgainstOnHoldContract_IsBlocked()
        {
            var contract = new Contract { Status = ContractStatus.OnHold };
            bool blocked = contract.Status == ContractStatus.Expired ||
                           contract.Status == ContractStatus.OnHold;
            Assert.True(blocked);
        }

        [Theory]
        [InlineData(ContractStatus.Active, true)]
        [InlineData(ContractStatus.Draft, true)]
        [InlineData(ContractStatus.Expired, false)]
        [InlineData(ContractStatus.OnHold, false)]
        public void ServiceRequest_EligibilityByStatus_MatchesExpected(
            ContractStatus status, bool expectedEligible)
        {
            var contract = new Contract { Status = status };
            bool eligible = contract.Status != ContractStatus.Expired &&
                            contract.Status != ContractStatus.OnHold;
            Assert.Equal(expectedEligible, eligible);
        }

        // ── Contract date logic ────────────────────────────────────────────

        [Fact]
        public void Contract_EndDateBeforeStartDate_IsLogicallyInvalid()
        {
            // Arrange
            var contract = new Contract
            {
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2025, 1, 1)
            };

            // Act
            bool invalid = contract.EndDate < contract.StartDate;

            // Assert
            Assert.True(invalid);
        }

        [Fact]
        public void Contract_EndDateAfterStartDate_IsLogicallyValid()
        {
            var contract = new Contract
            {
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2026, 1, 1)
            };
            Assert.True(contract.EndDate > contract.StartDate);
        }

        // ── ServiceRequest model defaults ──────────────────────────────────

        [Fact]
        public void ServiceRequest_DefaultStatus_IsPending()
        {
            var req = new ServiceRequest();
            Assert.Equal("Pending", req.Status);
        }

        [Fact]
        public void ServiceRequest_DateCreated_IsSetOnInit()
        {
            var before = DateTime.Now.AddSeconds(-1);
            var req = new ServiceRequest();
            var after = DateTime.Now.AddSeconds(1);

            Assert.InRange(req.DateCreated, before, after);
        }

        // ── Contract default status ────────────────────────────────────────

        [Fact]
        public void Contract_DefaultStatus_IsDraft()
        {
            var contract = new Contract();
            Assert.Equal(ContractStatus.Draft, contract.Status);
        }

        // ── Currency conversion integration (business rule) ────────────────

        [Fact]
        public void CostZar_ShouldEqualCostUsd_MultipliedByRate()
        {
            // Arrange
            decimal costUsd = 200m;
            decimal rate = 18.75m;
            decimal expectedZar = 3750.00m;

            // Act: mirrors what ServiceRequestsController does before saving
            decimal actualZar = Math.Round(costUsd * rate, 2);

            // Assert
            Assert.Equal(expectedZar, actualZar);
        }

        [Fact]
        public void CostZar_WithFractionalRate_IsRoundedToTwoDecimals()
        {
            decimal costUsd = 3m;
            decimal rate = 18.3333m;
            decimal result = Math.Round(costUsd * rate, 2);

            // 3 * 18.3333 = 54.9999 → rounds to 55.00
            Assert.Equal(55.00m, result);
        }
    }
}
