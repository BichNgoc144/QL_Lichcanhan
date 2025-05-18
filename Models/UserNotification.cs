using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace QL_Lichcanhan.Models
{
    public class UserNotification
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }

        public int AppointmentId { get; set; }

        [ForeignKey("AppointmentId")]
        public Appointment Appointment { get; set; }

        public string Message { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsRead { get; set; } = false;

        public bool IsReminder { get; set; } = false;

        public DateTime? ReminderTime { get; set; }
    }
}
