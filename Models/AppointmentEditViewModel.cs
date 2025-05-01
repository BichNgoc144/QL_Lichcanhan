namespace QL_Lichcanhan.Models
{
    public class AppointmentEditViewModel
    {

        public Appointment Appointment { get; set; }
        public string EnteredEmails { get; set; }
        public List<string> CurrentParticipantEmails { get; set; }
    }
}
