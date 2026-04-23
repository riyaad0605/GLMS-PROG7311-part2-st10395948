using System.ComponentModel.DataAnnotations;

namespace GLMS.Web.Models
{
    public class Client
    {
        public int ClientId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Client Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Contact Details")]
        public string ContactDetails { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Region { get; set; } = string.Empty;

        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}
