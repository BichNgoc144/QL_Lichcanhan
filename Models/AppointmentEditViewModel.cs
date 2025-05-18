using System.ComponentModel.DataAnnotations;

namespace QL_Lichcanhan.Models
{
    public class AppointmentEditViewModel
    {

        public Appointment Appointment { get; set; }
        public string EnteredEmails { get; set; }
        public List<string> CurrentParticipantEmails { get; set; }
        [Display(Name = "Nhắc trước (phút)")]
        [Range(0, int.MaxValue, ErrorMessage = "Vui lòng nhập số phút hợp lệ")]
        public int ReminderMinutesBefore { get; set; }
    }
}
