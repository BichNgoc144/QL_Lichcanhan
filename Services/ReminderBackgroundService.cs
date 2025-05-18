using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QL_Lichcanhan.Data;
using QL_Lichcanhan.Models;
using QL_Lichcanhan.Hubs;  // Thêm để dùng NotificationHub

namespace QL_Lichcanhan.Services
{
    public class ReminderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReminderBackgroundService> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ReminderBackgroundService> logger,
            IHubContext<NotificationHub> hubContext)  // Inject thêm SignalR hub
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReminderBackgroundService is running.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var now = DateTime.UtcNow;

                        var upcomingAppointments = await context.Appointments
                            .Include(a => a.Reminder)
                            .Include(a => a.GroupParticipants)
                            .Where(a => a.Reminder != null && a.StartTime > now)
                            .ToListAsync(stoppingToken);

                        foreach (var appointment in upcomingAppointments)
                        {
                            var reminderTime = appointment.StartTime - appointment.Reminder.TimeBefore;

                            if (reminderTime <= now)
                            {
                                var userIds = new List<string> { appointment.UserId };
                                userIds.AddRange(appointment.GroupParticipants.Select(gp => gp.UserId));

                                foreach (var userId in userIds.Distinct())
                                {
                                    bool alreadyNotified = await context.UserNotifications.AnyAsync(un =>
                                        un.AppointmentId == appointment.Id && un.UserId == userId && un.IsReminder,
                                        stoppingToken);

                                    if (!alreadyNotified)
                                    {
                                        var message = $"⏰ Sắp đến cuộc hẹn: {appointment.Name} lúc {appointment.StartTime:HH:mm dd/MM/yyyy}";

                                        var notification = new UserNotification
                                        {
                                            UserId = userId,
                                            AppointmentId = appointment.Id,
                                            Message = message,
                                            CreatedAt = now,
                                            IsReminder = true,
                                            ReminderTime = reminderTime
                                        };

                                        context.UserNotifications.Add(notification);

                                        // Gửi thông báo realtime đến client đã kết nối
                                        await _hubContext.Clients.User(userId)
                                            .SendAsync("ReceiveNotification", message);
                                    }
                                }
                            }
                        }

                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi nhắc trong ReminderBackgroundService");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
