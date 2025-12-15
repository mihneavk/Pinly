using System.ComponentModel.DataAnnotations;

namespace Pinly.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Numele grupului este obligatoriu")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Descrierea grupului este obligatorie")]
        public string Description { get; set; }

        public DateTime CreatedDate { get; set; }

        // Relația 1:N: Cine a creat/modereaza grupul
        public string ModeratorId { get; set; } // FK
        public virtual ApplicationUser? Moderator { get; set; }

        // Relații de Navigare
        public virtual ICollection<GroupMessage>? Messages { get; set; }
        public virtual ICollection<GroupMembership>? Memberships { get; set; }
    }
}