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
            var appointments = _context.Appointments
                .Where(a => a.UserId == user.Id)
                .ToList();
            return View(appointments);
        }

        // Hiển thị form tạo cuộc hẹn
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // Xử lý khi người dùng submit form tạo cuộc hẹn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            var user = await _userManager.GetUserAsync(User);

            // Kiểm tra thời gian không hợp lệ
            if (appointment.EndTime <= appointment.StartTime)
            {
                ModelState.AddModelError("", "End time must be after start time.");
                return View(appointment);
            }

            // Kiểm tra trùng khung giờ với cuộc hẹn khác
            bool conflict = _context.Appointments.Any(a =>
                a.UserId == user.Id &&
                a.StartTime < appointment.EndTime &&
                a.EndTime > appointment.StartTime);

            if (conflict)
            {
                ModelState.AddModelError("", "You already have another appointment at this time.");
                return View(appointment);
            }

            // Nếu không trùng thì lưu
            appointment.UserId = user.Id;
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // GET: Appointment/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null || appointment.UserId != _userManager.GetUserId(User))
                return Unauthorized();

            return View(appointment);
        }

        // POST: Appointment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Appointment appointment)
        {
            if (id != appointment.Id) return NotFound();

            var userId = _userManager.GetUserId(User);
            appointment.UserId = userId;

            if (ModelState.IsValid)
            {
                _context.Update(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(appointment);
        }

        // GET: Appointment/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == _userManager.GetUserId(User));
            if (appointment == null) return Unauthorized();

            return View(appointment);
        }

        // POST: Appointment/Delete/5
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

    }

}
