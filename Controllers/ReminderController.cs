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
        var now = DateTime.Now;

        // 1. Cuộc hẹn cá nhân sắp diễn ra
        var individualAppointments = await _context.Appointments
            .Where(a => a.UserId == userId &&
                        !a.IsGroupMeeting &&
                        a.StartTime > now &&
                        a.StartTime <= now.AddMinutes(10))
            .ToListAsync();

        // 2. Cuộc hẹn nhóm có user tham gia
        var groupAppointments = await _context.Appointments
            .Include(a => a.GroupParticipants)
            .Where(a => a.IsGroupMeeting &&
                        a.StartTime > now &&
                        a.StartTime <= now.AddMinutes(10) &&
                        a.GroupParticipants.Any(g => g.UserId == userId))
            .ToListAsync();

        var allAppointments = individualAppointments.Concat(groupAppointments).ToList();

        // 3. Thêm nhắc hẹn nếu chưa có
        foreach (var appt in allAppointments)
        {
            bool alreadyHas = await _context.UserNotifications
                .AnyAsync(n => n.AppointmentId == appt.Id && n.UserId == userId);

            if (!alreadyHas)
            {
                var notify = new UserNotification
                {
                    UserId = userId,
                    AppointmentId = appt.Id,
                    Message = $"Bạn có cuộc hẹn sắp diễn ra: {appt.Name}",
                    ReminderTime = now,
                    IsRead = false
                };
                _context.UserNotifications.Add(notify);
            }
        }

        // 4. Xóa các nhắc hẹn đã quá thời gian kết thúc
        var expiredNoti = await _context.UserNotifications
            .Include(n => n.Appointment)
            .Where(n => n.Appointment.EndTime < now)
            .ToListAsync();

        _context.UserNotifications.RemoveRange(expiredNoti);

        await _context.SaveChangesAsync();

        // 5. Trả danh sách nhắc hẹn hiện tại
        var notifications = await _context.UserNotifications
            .Include(n => n.Appointment)
            .Where(n => n.UserId == userId)
            .OrderBy(n => n.Appointment.StartTime)
            .ToListAsync();

        return View(notifications);
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var noti = await _context.UserNotifications.FindAsync(id);
        if (noti != null)
        {
            noti.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}

