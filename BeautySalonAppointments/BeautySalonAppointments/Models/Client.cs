using System.ComponentModel.DataAnnotations;

namespace BeautySalonAppointments.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(9)]
        public string PhoneNumber { get; set; }
    }
}
