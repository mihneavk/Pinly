using System.ComponentModel.DataAnnotations;

namespace Pinly.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Conținutul comentariului nu poate fi gol")]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; }

        // Relația 1:N cu Pin (La ce Pin este comentariul)
        public int PinId { get; set; } // FK
        public virtual Pin? Pin { get; set; }

        // Relația 1:N cu ApplicationUser (Autorul comentariului)
        public string ApplicationUserId { get; set; } // FK
        public virtual ApplicationUser? ApplicationUser { get; set; }
        
    }
}