using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautySalonAppointments.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Podaj datę wizyty.")]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public DateTime Date { get; set; }

        public int Duration { get; set; } = 60;

        [Required(ErrorMessage = "Podaj nazwę usługi.")]
        [StringLength(200)]
        public string Service { get; set; }

        [Required(ErrorMessage = "Wybierz klienta.")]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public Client? Client { get; set; }
    }
}

