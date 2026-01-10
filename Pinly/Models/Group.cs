using System.ComponentModel.DataAnnotations;

namespace Pinly.Models
{
    public class Group
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Description { get; set; } // Optional pt DM

        public bool IsPrivate { get; set; } = false; // False=Grup, True=DM
        public bool IsPublic { get; set; } = false; // true - grup public si false pt privat

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? ModeratorId { get; set; }
        public virtual ApplicationUser? Moderator { get; set; }

        public virtual ICollection<GroupMessage>? Messages { get; set; }
        public virtual ICollection<GroupMembership>? Memberships { get; set; }
    }
}