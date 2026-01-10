using System.ComponentModel.DataAnnotations;

namespace Pinly.Models
{
    public class GroupMembership
    {
        public int GroupId { get; set; }
        public virtual Group Group { get; set; }

        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.Now;
        public bool IsAccepted { get; set; } = true;
        public bool IsModerator { get; set; } = false;
        public bool IsBlocked { get; set; } = false;
    }
}