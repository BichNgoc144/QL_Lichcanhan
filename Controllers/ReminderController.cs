//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using QL_Lichcanhan.Data;
//using System.Security.Claims;

//namespace QL_Lichcanhan.Controllers
//{
//    [Authorize]
//    public class ReminderController : Controller
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly UserManager<IdentityUser> _userManager;

//        public ReminderController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
//        {
//            _context = context;
//            _userManager = userManager;
//        }

//        public async Task<IActionResult> Index()
//        {
//            var userId = _userManager.GetUserId(User);
//            var now = DateTime.Now;

//            // Lấy các nhắc hẹn thuộc về người dùng
//            var userReminders = await _context.UserNotifications
//                .Include(un => un.Appointment)
//                .Where(un => un.Appointment.UserId == userId)
//                .ToListAsync();

//            // Xóa các nhắc hẹn nếu cuộc hẹn đã kết thúc
//            var expired = userReminders
//                .Where(un => un.Appointment.EndTime <= now)
//                .ToList();

//            if (expired.Any())
//            {
//                _context.UserNotifications.RemoveRange(expired);
//                await _context.SaveChangesAsync();
//            }

//            // Lọc các nhắc hẹn hợp lệ (còn hiệu lực và gần đến thời gian bắt đầu)
//            var validReminders = userReminders
//                .Where(un =>
//                    un.ReminderTime <= now &&                      // Đã đến lúc nhắc
//                    (un.Appointment.StartTime - now).TotalMinutes <= 10 &&  // Gần thời gian bắt đầu
//                    un.Appointment.EndTime > now                  // Cuộc hẹn còn hiệu lực
//                )
//                .ToList();

//            return View(validReminders);
//        }


//        [HttpPost]
//        public async Task<IActionResult> MarkAsRead(int id)
//        {
//            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            var reminder = await _context.UserNotifications
//                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

//            if (reminder == null)
//                return NotFound();

//            reminder.IsRead = true;
//            await _context.SaveChangesAsync();
//            return Ok();
//        }
//    }
//}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using QL_Lichcanhan.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using QL_Lichcanhan.Data;

public class ReminderController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public ReminderController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        // Lấy tất cả các cuộc hẹn sắp diễn ra trong 10 phút tới
        var now = DateTime.Now;
        var upcomingAppointments = await _context.Appointments
            .Where(a => a.UserId == userId && !a.IsGroupMeeting &&
                        a.StartTime > now && a.StartTime <= now.AddMinutes(10))
            .ToListAsync();

        // Thêm nhắc hẹn nếu chưa tồn tại
        foreach (var appt in upcomingAppointments)
        {
            var exists = await _context.UserNotifications
                .AnyAsync(n => n.AppointmentId == appt.Id && n.UserId == userId);

            if (!exists)
            {
                var notification = new UserNotification
                {
                    UserId = userId,
                    AppointmentId = appt.Id,
                    Message = $"Bạn có cuộc hẹn sắp diễn ra: {appt.Name}",
                    ReminderTime = now,
                    IsRead = false
                };

                _context.UserNotifications.Add(notification);
            }
        }

        // Xóa nhắc hẹn đã quá thời gian kết thúc
        var expired = await _context.UserNotifications
            .Include(n => n.Appointment)
            .Where(n => n.Appointment.EndTime < now)
            .ToListAsync();

        _context.UserNotifications.RemoveRange(expired);

        await _context.SaveChangesAsync();

        // Trả về danh sách nhắc hẹn
        var reminders = await _context.UserNotifications
            .Include(n => n.Appointment)
            .Where(n => n.UserId == userId)
            .OrderBy(n => n.Appointment.StartTime)
            .ToListAsync();

        return View(reminders);
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var notify = await _context.UserNotifications.FindAsync(id);
        if (notify != null)
        {
            notify.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
