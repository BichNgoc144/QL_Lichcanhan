﻿using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using QL_Lichcanhan.Models;

namespace QL_Lichcanhan.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Location { get; set; }

        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }

        [Display(Name = "End Time")]
        public DateTime EndTime { get; set; }

        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }

        public Reminder? Reminder { get; set; }

        public bool IsGroupMeeting { get; set; } = false;

        public ICollection<GroupParticipant> GroupParticipants { get; set; }

        public string? Note { get; set; }
    }

    public class GroupParticipant
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public string UserId { get; set; }
        public Appointment Appointment { get; set; }
        public IdentityUser User { get; set; }
    }

}
