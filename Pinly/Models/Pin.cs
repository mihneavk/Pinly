using System.ComponentModel.DataAnnotations;
using Pinly.Models;

namespace Pinly.Models
{
    public class Pin
    {
        [Key]
        public int Id { get; set; }
        
        
        [Required(ErrorMessage = "Titlul este obligatoriu")]
        public string Title { get; set; }
        
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Conținutul media este obligatoriu")]
        public string MediaPath { get; set; }
        
        public DateTime CreatedDate { get; set; }

        public string? ApplicationUserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        
        //Relatii 1:N cu entitati dependente (comentarii si reactii)
        
        public virtual ICollection<Comment>? Comments { get; set; }
        public virtual ICollection<Reaction>? Reactions { get; set; }
    }
}