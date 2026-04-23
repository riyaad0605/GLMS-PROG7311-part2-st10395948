using System.ComponentModel.DataAnnotations;

namespace GLMS.Web.Models
{
    // ──────────────────────────────────────────────
    // Contract search / filter
    // ──────────────────────────────────────────────
    public class ContractSearchViewModel
    {
        [DataType(DataType.Date)]
        [Display(Name = "From Date")]
        public DateTime? StartDateFrom { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "To Date")]
        public DateTime? StartDateTo { get; set; }

        [Display(Name = "Status")]
        public ContractStatus? StatusFilter { get; set; }

        public List<Contract> Results { get; set; } = new();
    }

    // ──────────────────────────────────────────────
    // Service request creation
    // ──────────────────────────────────────────────
    public class ServiceRequestCreateViewModel
    {
        [Required]
        [Display(Name = "Contract")]
        public int ContractId { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Cost (USD)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Cost must be greater than zero.")]
        public decimal CostUsd { get; set; }

        [Display(Name = "Live Exchange Rate (1 USD → ZAR)")]
        public decimal ExchangeRate { get; set; }

        [Display(Name = "Estimated Cost (ZAR)")]
        public decimal EstimatedCostZar { get; set; }
    }

    // ──────────────────────────────────────────────
    // Login
    // ──────────────────────────────────────────────
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    // ──────────────────────────────────────────────
    // Register (Admin creates accounts)
    // ──────────────────────────────────────────────
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } = "Manager";
    }
}
