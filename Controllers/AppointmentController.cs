using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QL_Lichcanhan.Data;
using QL_Lichcanhan.Models;

namespace QL_Lichcanhan.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AppointmentController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

     
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var now = DateTime.Now;

            var personalAppointments = await _context.Appointments
                .Include(a => a.Reminder)
                .Where(a => a.UserId == user.Id && !a.IsGroupMeeting && a.EndTime > now)
                .ToListAsync();

            var groupAppointments = await _context.GroupParticipants
                .Include(g => g.Appointment)
                .ThenInclude(a => a.Reminder)
                .Where(g => g.UserId == user.Id && g.Appointment.EndTime > now)
                .Select(g => g.Appointment)
                .ToListAsync();

            // Kiểm tra các lời nhắc
            List<string> reminderMessages = new();
            DateTime current = DateTime.Now;

            var allAppointments = personalAppointments.Concat(groupAppointments).Distinct();

            foreach (var appt in allAppointments)
            {
                if (appt.Reminder != null)
                {
                    var reminderTime = appt.StartTime - appt.Reminder.TimeBefore;
                    if (reminderTime <= current && appt.StartTime > current)
                    {
                        reminderMessages.Add($"Bạn có cuộc hẹn '{appt.Name}' lúc {appt.StartTime:HH:mm dd/MM/yyyy} tại {appt.Location}.");
                    }
                }
            }

            var viewModel = new AppointmentIndexViewModel
            {
                PersonalAppointments = personalAppointments,
                GroupAppointments = groupAppointments,
                ReminderMessages = reminderMessages
            };

            return View(viewModel);
        }



        public IActionResult Create()
        {
            var model = new AppointmentCreateViewModel
            {
                Appointment = new Appointment()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppointmentCreateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var appointment = model.Appointment;

            if (appointment.EndTime <= appointment.StartTime)
            {
                ModelState.AddModelError("", "Thời gian kết thúc phải sau thời gian bắt đầu.");
                return View(model);
            }

            bool personalConflict = _context.Appointments.Any(a =>
                a.UserId == user.Id &&
                a.StartTime < appointment.EndTime &&
                a.EndTime > appointment.StartTime);

            if (personalConflict)
            {
                ModelState.AddModelError("", "Bạn đã có lịch cá nhân trùng thời gian.");
                return View(model);
            }

            var groupConflict = await _context.GroupParticipants
                .Include(g => g.Appointment)
                .Where(g => g.UserId == user.Id &&
                            g.Appointment.StartTime < appointment.EndTime &&
                            g.Appointment.EndTime > appointment.StartTime)
                .FirstOrDefaultAsync();

            if (groupConflict != null && !model.ForceSave)
            {
                ViewBag.ConflictMessage = "Bạn đang có cuộc họp nhóm trùng giờ. Bạn có muốn tham gia cuộc họp nhóm không?";
                return View(model);
            }

            if (groupConflict != null && model.ForceSave)
            {
                _context.GroupParticipants.Remove(groupConflict);
                await _context.SaveChangesAsync();
            }

            List<IdentityUser> validUsers = new();
            if (!string.IsNullOrWhiteSpace(model.EnteredEmails))
            {
                var emails = model.EnteredEmails
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim().ToLower())
                    .Distinct();

                foreach (var email in emails)
                {
                    var foundUser = await _userManager.FindByEmailAsync(email);
                    if (foundUser == null)
                    {
                        ModelState.AddModelError("", $"Không tìm thấy người dùng với email: {email}");
                        return View(model);
                    }
                    if (foundUser.Id != user.Id)
                    {
                        validUsers.Add(foundUser);
                    }
                }
            }

            appointment.UserId = user.Id;
            appointment.IsGroupMeeting = validUsers.Any();
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            //them nhac nho
            if (model.ReminderMinutesBefore > 0)
            {
                var reminder = new Reminder
                {
                    AppointmentId = model.Appointment.Id,
                    TimeBefore = TimeSpan.FromMinutes(model.ReminderMinutesBefore)
                };
                _context.Reminders.Add(reminder);
                await _context.SaveChangesAsync();
            }


            if (appointment.IsGroupMeeting)
            {
                // Thêm người tạo vào danh sách tham gia nhóm
                _context.GroupParticipants.Add(new GroupParticipant
                {
                    AppointmentId = appointment.Id,
                    UserId = user.Id
                });

                // Thêm các người dùng khác
                foreach (var u in validUsers)
                {
                    _context.GroupParticipants.Add(new GroupParticipant
                    {
                        AppointmentId = appointment.Id,
                        UserId = u.Id
                    });
                }

                await _context.SaveChangesAsync();
            }


            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Reminder) // load luôn reminder
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            bool isOwner = appointment.UserId == userId;
            bool isParticipant = await _context.GroupParticipants.AnyAsync(g => g.AppointmentId == id && g.UserId == userId);

            if (!isOwner && !isParticipant) return Unauthorized();

            var participantEmails = await _context.GroupParticipants
                .Where(g => g.AppointmentId == appointment.Id)
                .Select(g => g.User.Email)
                .ToListAsync();

            var viewModel = new AppointmentEditViewModel
            {
                Appointment = appointment,
                CurrentParticipantEmails = participantEmails,
                ReminderMinutesBefore = appointment.Reminder != null ? (int)appointment.Reminder.TimeBefore.TotalMinutes : 0
            };

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AppointmentEditViewModel model)
        {
            if (id != model.Appointment.Id) return NotFound();

            var userId = _userManager.GetUserId(User);
            var appointment = await _context.Appointments
                .Include(a => a.Reminder)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return NotFound();

            bool isOwner = appointment.UserId == userId;
            if (!isOwner) return Unauthorized();

            appointment.Name = model.Appointment.Name;
            appointment.Location = model.Appointment.Location;
            appointment.StartTime = model.Appointment.StartTime;
            appointment.EndTime = model.Appointment.EndTime;

            // Cập nhật nhóm người tham gia như trước
            if (!string.IsNullOrWhiteSpace(model.EnteredEmails))
            {
                var emails = model.EnteredEmails
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim().ToLower())
                    .Distinct();

                foreach (var email in emails)
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user != null && user.Id != userId)
                    {
                        bool alreadyParticipant = await _context.GroupParticipants
                            .AnyAsync(g => g.AppointmentId == id && g.UserId == user.Id);
                        if (!alreadyParticipant)
                        {
                            _context.GroupParticipants.Add(new GroupParticipant
                            {
                                AppointmentId = id,
                                UserId = user.Id
                            });
                        }
                    }
                }
            }

            // Xử lý reminder
            var existingReminder = appointment.Reminder;
            if (model.ReminderMinutesBefore > 0)
            {
                if (existingReminder != null)
                {
                    existingReminder.TimeBefore = TimeSpan.FromMinutes(model.ReminderMinutesBefore);
                    _context.Reminders.Update(existingReminder);
                }
                else
                {
                    var newReminder = new Reminder
                    {
                        AppointmentId = id,
                        TimeBefore = TimeSpan.FromMinutes(model.ReminderMinutesBefore)
                    };
                    _context.Reminders.Add(newReminder);
                }
            }
            else
            {
                if (existingReminder != null)
                {
                    _context.Reminders.Remove(existingReminder);
                }
            }

            _context.Update(appointment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == _userManager.GetUserId(User));
            if (appointment == null) return Unauthorized();

            return View(appointment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment?.UserId != _userManager.GetUserId(User)) return Unauthorized();

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> History()
        {
            var user = await _userManager.GetUserAsync(User);
            var now = DateTime.Now;

            var personalAppointments = await _context.Appointments
                .Where(a => a.UserId == user.Id && !a.IsGroupMeeting && a.EndTime <= now)
                .ToListAsync();

            var groupAppointments = await _context.GroupParticipants
                .Include(g => g.Appointment)
                .Where(g => g.UserId == user.Id && g.Appointment.EndTime <= now)
                .Select(g => g.Appointment)
                .ToListAsync();

            var viewModel = new AppointmentIndexViewModel
            {
                PersonalAppointments = personalAppointments,
                GroupAppointments = groupAppointments
            };

            return View(viewModel); 
        }

    }
}
