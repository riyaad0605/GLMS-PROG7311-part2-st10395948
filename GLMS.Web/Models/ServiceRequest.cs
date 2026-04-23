using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GLMS.Web.Models
{
    public class ServiceRequest
    {
        public int ServiceRequestId { get; set; }

        [Required]
        [Display(Name = "Contract")]
        public int ContractId { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Cost (USD)")]
        public decimal CostUsd { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Cost (ZAR)")]
        public decimal CostZar { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [Display(Name = "Date Created")]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        [ForeignKey("ContractId")]
        public Contract? Contract { get; set; }
    }
}
