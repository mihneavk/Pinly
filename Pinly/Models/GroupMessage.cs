using System.ComponentModel.DataAnnotations;

namespace Pinly.Models
{
    public class GroupMessage
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Conținutul mesajului nu poate fi gol")]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; }

        // Relația 1:N cu Group
        public int GroupId { get; set; } // FK
        public virtual Group? Group { get; set; }

        // Relația 1:N cu ApplicationUser (Autorul mesajului)
        public string ApplicationUserId { get; set; } // FK
        public virtual ApplicationUser? ApplicationUser { get; set; }
    }
}