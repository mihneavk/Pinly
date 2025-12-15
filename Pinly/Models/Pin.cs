using System.ComponentModel.DataAnnotations;
using Pinly.Models;

namespace Pinly.Models
{
    public class Pin
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }
        public string? Description { get; set; }
        public string ImagePath { get; set; }
        public DateTime CreatedDate { get; set; }

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
    }
}