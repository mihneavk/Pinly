using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pinly.Models
{
    public class Pin
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Titlul este obligatoriu")]
        public string Title { get; set; }

        public string? Description { get; set; }

        // Păstrăm MediaPath (e bine, poate vei vrea și video pe viitor)
        [Required(ErrorMessage = "Conținutul media este obligatoriu")]
        public string MediaPath { get; set; }

        public DateTime CreatedDate { get; set; }

        // --- MODIFICARE 1: Foreign Key ---
        // Scoatem '?' pentru că un Pin TREBUIE să aibă un autor.
        public string ApplicationUserId { get; set; }

        // --- MODIFICARE 2: Naming Convention ---
        // Redenumim 'User' în 'ApplicationUser' pentru a fi identic cu Comment.cs și GroupMessage.cs
        public virtual ApplicationUser? ApplicationUser { get; set; }

        public virtual ICollection<Comment>? Comments { get; set; }
        public virtual ICollection<Reaction>? Reactions { get; set; }
    }
}