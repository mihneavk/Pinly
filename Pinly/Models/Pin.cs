using System;
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

        [Required]
        public string ImagePath { get; set; }

        public DateTime CreatedDate { get; set; }

        public int? UserId { get; set; }

        public virtual User? User { get; set; }
    }
}