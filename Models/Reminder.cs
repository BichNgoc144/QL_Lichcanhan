using System.ComponentModel.DataAnnotations.Schema;
using QL_Lichcanhan.Models;

namespace QL_Lichcanhan.Models
{
    public class Reminder
    {
        public int Id { get; set; }

        public TimeSpan TimeBefore { get; set; }

        public int AppointmentId { get; set; }

        [ForeignKey("AppointmentId")]
        public Appointment Appointment { get; set; }
    }

}
