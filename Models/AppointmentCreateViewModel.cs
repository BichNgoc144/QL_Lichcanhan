using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace QL_Lichcanhan.Models
{
    public class AppointmentCreateViewModel
    {
        public Appointment Appointment { get; set; }

        // Cờ dùng để xác định nếu người dùng chọn “Không tham gia nhóm”
        public bool ForceSave { get; set; } = false;


        // ✅ Danh sách user được chọn để mời vào cuộc hẹn nhóm
        public List<string>? SelectedUserIds { get; set; }

        // ✅ Danh sách tất cả user để hiển thị trong form
        public List<IdentityUser>? AllUsers { get; set; }

        // ✅ Danh sách email đã nhập trong form
        public string? EnteredEmails { get; set; }
        [Display(Name = "Nhắc trước (phút)")]
        [Range(0, int.MaxValue, ErrorMessage = "Vui lòng nhập số phút hợp lệ")]
        public int ReminderMinutesBefore { get; set; }
    }
}
