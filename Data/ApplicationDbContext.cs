using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QL_Lichcanhan.Models;

namespace QL_Lichcanhan.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<GroupParticipant> GroupParticipants { get; set; }

        public DbSet<UserNotification> UserNotifications { get; set; }


    }
}
