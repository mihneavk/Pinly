using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Pinly.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; }

        // Relatie cu Pin
        public int PinId { get; set; }
        public virtual Pin Pin { get; set; }

        // Relatie cu User
        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }

        // LISTA DE LIKE-URI (NOU)
        public virtual ICollection<CommentLike> Likes { get; set; }
    }
}