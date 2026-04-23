using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GLMS.Web.Models
{
    public class Contract
    {
        public int ContractId { get; set; }

        [Required]
        [Display(Name = "Client")]
        public int ClientId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Required]
        public ContractStatus Status { get; set; } = ContractStatus.Draft;

        [Required]
        [StringLength(50)]
        [Display(Name = "Service Level")]
        public string ServiceLevel { get; set; } = string.Empty;

        [Display(Name = "Signed Agreement")]
        public string? SignedAgreementPath { get; set; }

        [ForeignKey("ClientId")]
        public Client? Client { get; set; }

        public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
    }
}
